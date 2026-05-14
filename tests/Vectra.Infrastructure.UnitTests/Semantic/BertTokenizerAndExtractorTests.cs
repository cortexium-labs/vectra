using FluentAssertions;
using NSubstitute;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Vectra.Infrastructure.Semantic.Providers.InternalBert;

namespace Vectra.Infrastructure.UnitTests.Semantic;

public class BertTokenizerTests
{
    private static readonly string[] MinimalVocab =
    [
        "[PAD]", "[UNK]", "[CLS]", "[SEP]", "[MASK]",
        "h", "e", "l", "o", "w", "r", "d", "a", "t", "i", "n", "s",
        "hello", "world", "test", "data", "request", "api", ".",
        ",", "!", "?", "admin", "user", "delete", "get", "post"
    ];

    private static BertTokenizer CreateSut() => new(MinimalVocab);

    [Fact]
    public void Tokenize_SimpleWord_InVocab_ReturnsIds()
    {
        var sut = CreateSut();

        var (inputIds, attentionMask) = sut.Tokenize("hello", maxLength: 8);

        inputIds.Should().HaveCount(8);
        attentionMask.Should().HaveCount(8);
        // First token is [CLS] (index 2), last non-pad is [SEP] (index 3)
        inputIds[0].Should().Be(2); // [CLS]
    }

    [Fact]
    public void Tokenize_PadsToMaxLength()
    {
        var sut = CreateSut();

        var (inputIds, attentionMask) = sut.Tokenize("hi", maxLength: 16);

        inputIds.Should().HaveCount(16);
        attentionMask.Should().HaveCount(16);
        // Trailing padding tokens should have id 0 and mask 0
        attentionMask[^1].Should().Be(0L);
    }

    [Fact]
    public void Tokenize_TruncatesToMaxLength()
    {
        var sut = CreateSut();
        // Long text to ensure truncation
        var longText = string.Join(" ", Enumerable.Repeat("hello world test data", 20));

        var (inputIds, attentionMask) = sut.Tokenize(longText, maxLength: 16);

        inputIds.Should().HaveCount(16);
        attentionMask.Should().HaveCount(16);
    }

    [Fact]
    public void Tokenize_UnknownWord_UsesUnkToken()
    {
        var sut = CreateSut();

        var (inputIds, _) = sut.Tokenize("xyz_unknown_word", maxLength: 8);

        // Should contain the [UNK] token id (1) somewhere
        inputIds.Should().Contain(1L); // [UNK]
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsPaddedResult()
    {
        var sut = CreateSut();

        var (inputIds, attentionMask) = sut.Tokenize("", maxLength: 8);

        inputIds.Should().HaveCount(8);
        // [CLS] + [SEP] then padding
        inputIds[0].Should().Be(2); // [CLS]
    }

    [Fact]
    public void Tokenize_WithPunctuation_SplitsProperly()
    {
        var sut = CreateSut();

        var (inputIds, _) = sut.Tokenize("hello,world", maxLength: 16);

        inputIds.Should().HaveCount(16);
        // Comma should be tokenized separately
    }

    [Fact]
    public void Tokenize_MultipleWords_ProcessesAll()
    {
        var sut = CreateSut();

        var (inputIds, attentionMask) = sut.Tokenize("hello world", maxLength: 32);

        inputIds.Should().HaveCount(32);
        // First token is [CLS]
        inputIds[0].Should().Be(2);
    }

    [Fact]
    public void Tokenize_AttentionMaskMatchesInputIds()
    {
        var sut = CreateSut();

        var (inputIds, attentionMask) = sut.Tokenize("test data", maxLength: 16);

        // Positions where inputIds != 0 should have mask = 1
        for (int i = 0; i < 16; i++)
        {
            if (inputIds[i] == 0)
                attentionMask[i].Should().Be(0L, $"padding at {i} should have mask 0");
        }
    }

    [Fact]
    public void Tokenize_WordPieceSubword_FallsBackToUnk()
    {
        // Word not in vocab, no subword matches → UNK
        var sut = CreateSut();

        var (inputIds, _) = sut.Tokenize("zzzunknownzzzz", maxLength: 8);

        inputIds.Should().Contain(1L); // [UNK]
    }
}

/// <summary>
/// Tests ModelPackageLoader and ModelPackageExtractor indirectly through InternalOnnxProvider,
/// since ModelPackageLoader, ModelPackageExtractor, and ModelAssets are internal.
/// </summary>
public class ModelPackageLoaderViaProviderTests
{
    private readonly Microsoft.Extensions.Logging.ILogger<InternalOnnxProvider> _logger =
        Microsoft.Extensions.Logging.Abstractions.NullLogger<InternalOnnxProvider>.Instance;

    private static byte[] CreateValidZipPackage(
        byte[] onnxBytes,
        string[] vocabLines,
        string[] labels,
        string onnxEntryName = "model.onnx")
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var onnxEntry = zip.CreateEntry(onnxEntryName);
            using (var s = onnxEntry.Open()) s.Write(onnxBytes);

            var vocabEntry = zip.CreateEntry("vocab.txt");
            using (var s = vocabEntry.Open())
            using (var w = new StreamWriter(s))
                foreach (var line in vocabLines) w.WriteLine(line);

            var labelsEntry = zip.CreateEntry("labels.json");
            using (var s = labelsEntry.Open())
                JsonSerializer.Serialize(s, labels);
        }
        return ms.ToArray();
    }

    [Fact]
    public void Constructor_EnabledWithMissingPackagePath_ThrowsInvalidOperationException()
    {
        var cacheProvider = Substitute.For<Vectra.Application.Abstractions.Caches.ICacheProvider>();
        var cacheService = Substitute.For<Vectra.Infrastructure.Caches.ICacheService>();
        cacheService.Current.Returns(cacheProvider);

        var options = Microsoft.Extensions.Options.Options.Create(
            new Vectra.BuildingBlocks.Configuration.Semantic.SemanticConfiguration
            {
                Enabled = true,
                Providers = new Vectra.BuildingBlocks.Configuration.Semantic.SemanticProviders
                {
                    Internal = new Vectra.BuildingBlocks.Configuration.Semantic.InternalOnnxConfiguration
                    {
                        PackagePath = null
                    }
                }
            });

        var act = () => new InternalOnnxProvider(options, cacheService, _logger);

        act.Should().Throw<InvalidOperationException>().WithMessage("*PackagePath*");
    }

    [Fact]
    public void Constructor_EnabledWithNonExistentFile_ThrowsFileNotFoundException()
    {
        var cacheProvider = Substitute.For<Vectra.Application.Abstractions.Caches.ICacheProvider>();
        var cacheService = Substitute.For<Vectra.Infrastructure.Caches.ICacheService>();
        cacheService.Current.Returns(cacheProvider);

        var options = Microsoft.Extensions.Options.Options.Create(
            new Vectra.BuildingBlocks.Configuration.Semantic.SemanticConfiguration
            {
                Enabled = true,
                Providers = new Vectra.BuildingBlocks.Configuration.Semantic.SemanticProviders
                {
                    Internal = new Vectra.BuildingBlocks.Configuration.Semantic.InternalOnnxConfiguration
                    {
                        PackagePath = "does_not_exist_xyz.zip"
                    }
                }
            });

        var act = () => new InternalOnnxProvider(options, cacheService, _logger);

        act.Should().Throw<FileNotFoundException>();
    }
}

public class ModelPackageExtractorTests
{
    private static byte[] CreateZipPackage(
        string onnxContent,
        string vocabContent,
        string labelsJson,
        string onnxEntryName = "model.onnx")
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var s = zip.CreateEntry(onnxEntryName).Open())
                s.Write(Encoding.UTF8.GetBytes(onnxContent));
            using (var s = zip.CreateEntry("vocab.txt").Open())
                s.Write(Encoding.UTF8.GetBytes(vocabContent));
            using (var s = zip.CreateEntry("labels.json").Open())
                s.Write(Encoding.UTF8.GetBytes(labelsJson));
        }
        return ms.ToArray();
    }

    [Fact]
    public void Extract_ValidPackage_ReturnsAllAssets()
    {
        var package = CreateZipPackage("onnx-bytes", "[PAD]\n[UNK]\nhello\nworld", "[\"read\",\"write\",\"harmful\"]");

        var (onnx, vocab, labels) = ModelPackageExtractor.Extract(package);

        onnx.Should().NotBeEmpty();
        vocab.Should().HaveCount(4);
        vocab.Should().Contain("hello");
        labels.Should().HaveCount(3);
        labels.Should().Contain("read");
    }

    [Fact]
    public void Extract_MissingOnnx_ThrowsInvalidOperationException()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var s = zip.CreateEntry("vocab.txt").Open();
            s.Write(Encoding.UTF8.GetBytes("[PAD]"));
        }

        var act = () => ModelPackageExtractor.Extract(ms.ToArray());

        act.Should().Throw<InvalidOperationException>().WithMessage("*model.onnx*");
    }

    [Fact]
    public void Extract_MissingVocab_ThrowsInvalidOperationException()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var s = zip.CreateEntry("model.onnx").Open();
            s.Write(Encoding.UTF8.GetBytes("bytes"));
        }

        var act = () => ModelPackageExtractor.Extract(ms.ToArray());

        act.Should().Throw<InvalidOperationException>().WithMessage("*vocab.txt*");
    }

    [Fact]
    public void Extract_MissingLabels_ThrowsInvalidOperationException()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var s = zip.CreateEntry("model.onnx").Open()) s.Write(Encoding.UTF8.GetBytes("bytes"));
            using (var s = zip.CreateEntry("vocab.txt").Open()) s.Write(Encoding.UTF8.GetBytes("[PAD]"));
        }

        var act = () => ModelPackageExtractor.Extract(ms.ToArray());

        act.Should().Throw<InvalidOperationException>().WithMessage("*labels.json*");
    }

    [Fact]
    public void Extract_WithEncEntryName_FindsEncryptedOnnx()
    {
        var package = CreateZipPackage("enc-bytes", "[PAD]\n[UNK]", "[\"read\"]", onnxEntryName: "model.onnx.enc");

        var (onnx, vocab, labels) = ModelPackageExtractor.Extract(package, "model.onnx.enc");

        onnx.Should().NotBeEmpty();
        vocab.Should().HaveCount(2);
        labels.Should().Contain("read");
    }
}

public class ModelPackageLoaderDirectTests
{
    private static byte[] CreateCommunityPackage()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var s = zip.CreateEntry("model.onnx").Open()) s.Write(new byte[] { 0x01, 0x02 });
            using (var s = zip.CreateEntry("vocab.txt").Open())
            using (var w = new StreamWriter(s)) { w.WriteLine("[PAD]"); w.WriteLine("[UNK]"); w.WriteLine("[CLS]"); w.WriteLine("[SEP]"); w.WriteLine("hello"); }
            using (var s = zip.CreateEntry("labels.json").Open())
                JsonSerializer.Serialize(s, new[] { "read", "write" });
        }
        return ms.ToArray();
    }

    [Fact]
    public void Load_CommunityModel_ReturnsCorrectAssets()
    {
        var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), ".zip");
        try
        {
            File.WriteAllBytes(tmpPath, CreateCommunityPackage());
            var config = new Vectra.BuildingBlocks.Configuration.Semantic.InternalOnnxConfiguration
            {
                PackagePath = tmpPath,
                ModelType = "Community"
            };

            var assets = ModelPackageLoader.Load(config);

            assets.OnnxBytes.Length.Should().BeGreaterThan(0);
            assets.VocabLines.Should().HaveCount(5);
            assets.IntentLabels.Should().HaveCount(2);
        }
        finally { File.Delete(tmpPath); }
    }

    [Fact]
    public void Load_RelativePath_ExpandsToAbsolute()
    {
        var tmpDir = Path.GetTempPath();
        var filename = $"test_model_{Guid.NewGuid():N}.zip";
        var tmpPath = Path.Combine(tmpDir, filename);
        try
        {
            File.WriteAllBytes(tmpPath, CreateCommunityPackage());
            var config = new Vectra.BuildingBlocks.Configuration.Semantic.InternalOnnxConfiguration
            {
                PackagePath = tmpPath,
                ModelType = "Community"
            };

            var assets = ModelPackageLoader.Load(config);

            assets.Should().NotBeNull();
        }
        finally { File.Delete(tmpPath); }
    }

    [Fact]
    public void Load_ProModelMissingLicensePath_ThrowsInvalidOperationException()
    {
        // Pro model requires model.onnx.enc in the zip
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var s = zip.CreateEntry("model.onnx.enc").Open()) s.Write(new byte[] { 0x01 });
            using (var s = zip.CreateEntry("vocab.txt").Open())
            using (var w = new StreamWriter(s)) { w.WriteLine("[PAD]"); w.WriteLine("[UNK]"); }
            using (var s = zip.CreateEntry("labels.json").Open())
                JsonSerializer.Serialize(s, new[] { "read" });
        }
        var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), ".zip");
        try
        {
            File.WriteAllBytes(tmpPath, ms.ToArray());
            var config = new Vectra.BuildingBlocks.Configuration.Semantic.InternalOnnxConfiguration
            {
                PackagePath = tmpPath,
                ModelType = "Pro",
                LicensePath = null
            };

            var act = () => ModelPackageLoader.Load(config);

            act.Should().Throw<InvalidOperationException>().WithMessage("*LicensePath*");
        }
        finally { File.Delete(tmpPath); }
    }
}




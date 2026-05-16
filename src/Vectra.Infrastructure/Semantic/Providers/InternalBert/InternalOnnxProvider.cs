using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.Semantic.Providers.InternalBert;

/// <summary>
/// ONNX-based semantic provider. Reads the model ZIP (Community or Pro) from the local
/// file system and loads all assets entirely in memory — no network, no temp files.
/// </summary>
public sealed class InternalOnnxProvider : ISemanticProvider, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly BertTokenizer? _tokenizer;
    private readonly string[] _intentLabels = [];
    private readonly int _maxLength;
    private readonly ICacheProvider? _cacheProvider;
    private readonly ILogger<InternalOnnxProvider> _logger;
    private readonly bool _enabled;

    public InternalOnnxProvider(
        IOptions<SemanticConfiguration> options,
        ICacheService cacheService,
        ILogger<InternalOnnxProvider> logger)
    {
        _logger  = logger ?? throw new ArgumentNullException(nameof(logger));
        var config = options.Value;

        _enabled = config.Enabled ?? false;

        if (!_enabled)
        {
            _logger.LogInformation("Semantic is disabled — skipping Internal ONNX model loading.");
            return;
        }

        var internalConfig = config.Providers.Internal;
        _maxLength     = internalConfig.MaxLength ?? 128;
        _cacheProvider = cacheService.Current ?? throw new ArgumentNullException(nameof(cacheService));

        _logger.LogInformation("Loading Internal ONNX model. Type={ModelType}", internalConfig.ModelType);

        var assets = ModelPackageLoader.Load(internalConfig);

        _session      = new InferenceSession(assets.OnnxBytes.ToArray(), new SessionOptions());
        _tokenizer    = new BertTokenizer(assets.VocabLines);
        _intentLabels = assets.IntentLabels;

        _logger.LogInformation("Internal ONNX model loaded successfully. Labels={Count}", _intentLabels.Length);
    }

    public async Task<SemanticAnalysisResult> AnalyzeAsync(
        string? body,
        string metadata,
        CancellationToken cancellationToken)
    {
        if (!_enabled)
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };

        if (string.IsNullOrWhiteSpace(body))
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };

        var cacheKey = $"semantic_internal:{ComputeHash(body)}";
        var (success, cached) = await _cacheProvider!.TryGetValueAsync<SemanticAnalysisResult>(cacheKey);
        if (success)
            return cached!;

        var (inputIds, attentionMask) = _tokenizer!.Tokenize(body, _maxLength);
        var inputTensor = new DenseTensor<long>(inputIds, new[] { 1, _maxLength });
        var maskTensor  = new DenseTensor<long>(attentionMask, new[] { 1, _maxLength });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids",      inputTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor)
        };

        using var results = _session!.Run(inputs);
        var logits     = results.First().AsTensor<float>().ToArray();
        var probs      = Softmax(logits);
        var maxIdx     = Array.IndexOf(probs, probs.Max());
        var intent     = _intentLabels[maxIdx];
        var confidence = probs[maxIdx];

        var riskTags = intent switch
        {
            "bulk_export"        => new[] { "data_exfiltration" },
            "destructive_delete" => new[] { "destructive" },
            "admin_action"       => new[] { "privilege_escalation" },
            "harmful"            => new[] { "malicious" },
            _                    => Array.Empty<string>()
        };

        var result = new SemanticAnalysisResult
        {
            Intent       = intent,
            Confidence   = confidence,
            RiskTags     = riskTags,
            FallbackSafe = confidence < 0.7,
            Explanation  = $"Internal ONNX: {intent} ({confidence:F2})"
        };

        await _cacheProvider.SetAsync(cacheKey, result);
        return result;
    }

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exp = logits.Select(x => (float)Math.Exp(x - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(x => x / sum).ToArray();
    }

    private static string ComputeHash(string input) =>
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    public void Dispose() => _session?.Dispose();
}
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Vectra.Infrastructure.Semantic.Providers.LocalBert;

public class BertTokenizer
{
    private readonly Dictionary<string, int> _vocab;
    public BertTokenizer(string vocabPath)
    {
        _vocab = File.ReadLines(vocabPath)
            .Select((line, idx) => (line, idx))
            .ToDictionary(x => x.line, x => x.idx);
    }

    public (Tensor<int> inputIds, Tensor<int> attentionMask) Tokenize(string text, int maxLength = 128)
    {
        // Basic tokenization (for production, use a proper tokenizer library)
        var tokens = new List<string> { "[CLS]" };
        tokens.AddRange(text.Split(' ').Select(w => _vocab.ContainsKey(w) ? w : "[UNK]"));
        tokens.Add("[SEP]");

        var inputIds = tokens.Select(t => _vocab.GetValueOrDefault(t, _vocab["[UNK]"])).ToArray();
        var attentionMask = inputIds.Select(_ => 1).ToArray();

        // Pad/truncate
        if (inputIds.Length > maxLength)
        {
            inputIds = inputIds.Take(maxLength).ToArray();
            attentionMask = attentionMask.Take(maxLength).ToArray();
        }
        else
        {
            var padLength = maxLength - inputIds.Length;
            inputIds = inputIds.Concat(Enumerable.Repeat(0, padLength)).ToArray();
            attentionMask = attentionMask.Concat(Enumerable.Repeat(0, padLength)).ToArray();
        }

        var shape = new[] { 1, maxLength };
        return (new DenseTensor<int>(inputIds, shape), new DenseTensor<int>(attentionMask, shape));
    }
}
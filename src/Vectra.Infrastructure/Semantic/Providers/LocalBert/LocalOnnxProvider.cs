using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System.Text;
using Vectra.Application.Abstractions.Executions;

namespace Vectra.Infrastructure.Semantic.Providers.LocalBert;

public class LocalOnnxProvider: ISemanticProvider
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocalOnnxProvider> _logger;
    private readonly string[] _intentLabels = { "safe_read", "safe_write", "bulk_export", "destructive_delete", "admin_action", "harmful" };

    public LocalOnnxProvider(IConfiguration config, IMemoryCache cache, ILogger<LocalOnnxProvider> logger)
    {
        var modelPath = config["Semantic:ModelPath"] ?? "Models/intent_model.onnx";
        _session = new InferenceSession(modelPath);
        _tokenizer = new BertTokenizer(config["Semantic:VocabPath"] ?? "Models/vocab.txt");
        _cache = cache;
        _logger = logger;
    }

    public async Task<SemanticAnalysisResult> AnalyzeAsync(string? body, string metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };

        // Cache key: hash of request body (exact match)
        var cacheKey = $"semantic:{Convert.ToBase64String(Encoding.UTF8.GetBytes(body))}";
        if (_cache.TryGetValue<SemanticAnalysisResult>(cacheKey, out var cached))
            return cached!;

        // Tokenize
        var (inputIds, attentionMask) = _tokenizer.Tokenize(body, maxLength: 128);

        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
        };
        using var results = _session.Run(inputs);
        var logits = results.First().AsTensor<float>().ToArray();

        // Softmax to probabilities
        var probs = Softmax(logits);
        var maxIdx = Array.IndexOf(probs, probs.Max());
        var intent = _intentLabels[maxIdx];
        var confidence = probs[maxIdx];

        // Determine risk tags based on intent
        var riskTags = intent switch
        {
            "bulk_export" => new[] { "data_exfiltration" },
            "destructive_delete" => new[] { "destructive" },
            "admin_action" => new[] { "privilege_escalation" },
            "harmful" => new[] { "malicious" },
            _ => Array.Empty<string>()
        };

        var result = new SemanticAnalysisResult
        {
            Intent = intent,
            Confidence = confidence,
            RiskTags = riskTags,
            FallbackSafe = confidence < 0.7,
            Explanation = $"ONNX model prediction: {intent} ({confidence:F2})"
        };

        // Cache for 5 minutes (since request bodies may repeat)
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exp = logits.Select(x => Math.Exp(x - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(x => (float)(x / sum)).ToArray();
    }
}

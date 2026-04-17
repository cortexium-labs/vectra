using System.Text.RegularExpressions;

namespace Vectra.Infrastructure.Semantic.Providers.LocalBert;

public class BertTokenizer
{
    private readonly Dictionary<string, int> _vocab;
    private readonly List<string> _tokenStrings;
    private readonly Regex _whitespaceRegex = new Regex(@"\s+", RegexOptions.None, TimeSpan.FromSeconds(3));

    public BertTokenizer(string vocabPath)
    {
        var lines = File.ReadAllLines(vocabPath);
        _vocab = new Dictionary<string, int>();
        for (int i = 0; i < lines.Length; i++)
            _vocab[lines[i]] = i;
        _tokenStrings = lines.ToList();
    }

    public (long[] InputIds, long[] AttentionMask) Tokenize(string text, int maxLength = 128)
    {
        // Basic cleaning
        text = text.Trim().ToLowerInvariant();
        // Split on whitespace and punctuation (simplified)
        var tokens = new List<string>();
        foreach (var word in _whitespaceRegex.Split(text))
        {
            if (string.IsNullOrEmpty(word)) continue;
            // Keep punctuation as separate tokens (simple approach)
            var chars = word.ToCharArray();
            foreach (var c in chars)
            {
                if (char.IsPunctuation(c))
                {
                    if (tokens.Count > 0 && !char.IsPunctuation(tokens.Last().Last()))
                        tokens.Add(c.ToString());
                    else
                        tokens.Add(c.ToString());
                }
                else
                {
                    // add to last token if it's not punctuation
                    if (tokens.Count > 0 && !char.IsPunctuation(tokens.Last().Last()))
                        tokens[^1] += c;
                    else
                        tokens.Add(c.ToString());
                }
            }
        }

        // WordPiece tokenization
        var wordPieceTokens = new List<string> { "[CLS]" };
        foreach (var token in tokens)
        {
            var subTokens = TokenizeWord(token);
            wordPieceTokens.AddRange(subTokens);
        }
        wordPieceTokens.Add("[SEP]");

        // Convert to ids
        var inputIds = wordPieceTokens.Select(t => (long)_vocab.GetValueOrDefault(t, _vocab["[UNK]"])).ToArray();
        var attentionMask = inputIds.Select(_ => 1L).ToArray();

        // Pad or truncate
        if (inputIds.Length > maxLength)
        {
            inputIds = inputIds.Take(maxLength).ToArray();
            attentionMask = attentionMask.Take(maxLength).ToArray();
        }
        else
        {
            var padLength = maxLength - inputIds.Length;
            inputIds = inputIds.Concat(Enumerable.Repeat(0L, padLength)).ToArray();
            attentionMask = attentionMask.Concat(Enumerable.Repeat(0L, padLength)).ToArray();
        }

        return (inputIds, attentionMask);
    }

    private List<string> TokenizeWord(string word)
    {
        var tokens = new List<string>();
        while (!string.IsNullOrEmpty(word))
        {
            var found = false;
            for (int i = word.Length; i >= 1; i--)
            {
                var sub = word.Substring(0, i);
                if (_vocab.ContainsKey(sub))
                {
                    tokens.Add(sub);
                    word = word.Substring(i);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tokens.Add("[UNK]");
                break;
            }
        }
        return tokens;
    }
}
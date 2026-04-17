namespace Vectra.Application.Abstractions.Caches;

public interface ICacheProvider
{
    Task<object?> GetAsync(object key);
    Task<TItem?> GetAsync<TItem>(object key);
    Task<TItem> SetAsync<TItem>(object key, TItem value);
    Task<(bool success, TItem? value)> TryGetValueAsync<TItem>(string key);
    Task RemoveAsync(object key);
}
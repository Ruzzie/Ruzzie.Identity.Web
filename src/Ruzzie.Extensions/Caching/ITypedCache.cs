using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Ruzzie.Extensions.Caching;

public interface ITypedCache
{
    /// <summary>
    /// Gets the value from the cache if it exists, or adds it using the <paramref name="addFunc"/> to create the value, for a given key.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="addFunc">the function that is invoked when the value needs to be added to the cache</param>
    /// <param name="options">the options when the default needs to be overridden</param>
    /// <returns>The value</returns>
    T GetOrAdd<T>(string key, Func<string, T> addFunc, DistributedCacheEntryOptions? options = null);

    /// <summary>
    /// Gets the value from the cache if it exists, or adds it using the <paramref name="addFuncAsync"/> to create the value, for a given key.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="addFuncAsync">the function that is invoked when the value needs to be added to the cache</param>
    /// <param name="options">the options when the default needs to be overridden</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The value</returns>
    Task<T> GetOrAddAsync<T>(string                        key,
                             Func<string, Task<T>>         addFuncAsync,
                             DistributedCacheEntryOptions? options           = null,
                             CancellationToken             cancellationToken = default);

    T?       Get<T>(string      key);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    void Set<T>(string key, T value, DistributedCacheEntryOptions? options = null);


    Task SetAsync<T>(string                        key,
                     T                             value,
                     DistributedCacheEntryOptions? options           = null,
                     CancellationToken             cancellationToken = default);

    void Remove(string key);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
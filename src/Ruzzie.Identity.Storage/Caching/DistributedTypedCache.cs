using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Ruzzie.Identity.Storage.Caching
{
    /// <summary>
    /// A Distributed cache with given default KeyPrefix and default <see cref="DistributedCacheEntryOptions"/> that uses the <see cref="IDistributedCache"/> for storage.
    /// </summary>
    /// <remarks>This cache can be used for simple access (and basically functionally sharding) to Types that need to be cached. The values are binary Serialized with MessagePack. </remarks>
    public class DistributedTypedCache : ITypedCache
    {
        private readonly IDistributedCache            _distributedCache;
        private readonly ReadOnlyMemory<char>         _defaultKeyPrefix;
        private readonly DistributedCacheEntryOptions _defaultOptions;

        public DistributedTypedCache(IDistributedCache            distributedCache,
                                     string                       defaultKeyPrefix,
                                     DistributedCacheEntryOptions defaultOptions)
        {
            _distributedCache = distributedCache;
            _defaultOptions   = defaultOptions;
            _defaultKeyPrefix = defaultKeyPrefix.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string PrefixKey(ReadOnlySpan<char> key)
        {
            if (key.IsEmpty)
                return new string(_defaultKeyPrefix.Span);

            const int maxStackLimit = 1024;

            var keyLength = _defaultKeyPrefix.Length + key.Length;

            var keyWithPrefix = keyLength <= maxStackLimit ? stackalloc char[keyLength] : new char[keyLength];

            if (!_defaultKeyPrefix.IsEmpty)
                _defaultKeyPrefix.Span.CopyTo(keyWithPrefix);

            key.CopyTo(keyWithPrefix.Slice(_defaultKeyPrefix.Length));

            return new string(keyWithPrefix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] Serialize<T>(T value)
        {
            return MessagePack.MessagePackSerializer.Serialize(value,
                                                               MessagePack.Resolvers.ContractlessStandardResolver
                                                                          .Options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Deserialize<T>(in byte[] data)
        {
            return MessagePack.MessagePackSerializer.Deserialize<T>(data,
                                                                    MessagePack.Resolvers.ContractlessStandardResolver
                                                                               .Options);
        }

        public T GetOrAdd<T>(string key, Func<string, T> addFunc, DistributedCacheEntryOptions? options = null)
        {
            var cacheKey = PrefixKey(key);

            var entryFromCache = _distributedCache.Get(cacheKey);

            if (entryFromCache != null)
            {
                return Deserialize<T>(entryFromCache);
            }

            var value = addFunc(cacheKey);
            SetValue(cacheKey, value, options ?? _defaultOptions);

            return value;
        }

        private void SetValue<T>(string cacheKey, T value, DistributedCacheEntryOptions? options = null)
        {
            var valueToCache = Serialize(value);
            _distributedCache.Set(cacheKey, valueToCache, options);
        }

        private async Task SetValueAsync<T>(string                        cacheKey,
                                            T                             value,
                                            DistributedCacheEntryOptions? options           = null,
                                            CancellationToken             cancellationToken = default)
        {
            var valueToCache = Serialize(value);

            await _distributedCache.SetAsync(cacheKey, valueToCache, options, cancellationToken);
        }

        public async Task<T> GetOrAddAsync<T>(string                        key,
                                              Func<string, Task<T>>         addFuncAsync,
                                              DistributedCacheEntryOptions? options           = null,
                                              CancellationToken             cancellationToken = default)
        {
            var cacheKey = PrefixKey(key);

            var entryFromCache = await _distributedCache.GetAsync(cacheKey, cancellationToken);

            if (entryFromCache != null)
            {
                return Deserialize<T>(entryFromCache);
            }

            var value = await addFuncAsync(cacheKey);
            await SetValueAsync(cacheKey, value, options ?? _defaultOptions, cancellationToken);

            return value;
        }

        public T? Get<T>(string key)
        {
            var cacheKey = PrefixKey(key);

            var entryFromCache = _distributedCache.Get(cacheKey);
            if (entryFromCache == null || entryFromCache.Length == 0)
            {
                return default;
            }

            return Deserialize<T>(entryFromCache);
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = PrefixKey(key);

            var entryFromCache = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (entryFromCache == null || entryFromCache.Length == 0)
            {
                return default;
            }

            return Deserialize<T>(entryFromCache);
        }

        public void Set<T>(string key, T value, DistributedCacheEntryOptions? options = null)
        {
            var cacheKey = PrefixKey(key);
            SetValue(cacheKey, value, options ?? _defaultOptions);
        }

        public async Task SetAsync<T>(string                        key,
                                      T                             value,
                                      DistributedCacheEntryOptions? options           = null,
                                      CancellationToken             cancellationToken = default)
        {
            var cacheKey = PrefixKey(key);
            await SetValueAsync(cacheKey, value, options ?? _defaultOptions, cancellationToken);
        }

        public void Remove(string key)
        {
            var cacheKey = PrefixKey(key);
            _distributedCache.Remove(cacheKey);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = PrefixKey(key);
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Ruzzie.Extensions.Caching;

namespace Ruzzie.Extensions.UnitTests.Caching
{
    [TestFixture]
    public class DistributedTypedCacheTests
    {
        private DistributedTypedCache _cache = null!;


        [SetUp]
        public void Setup()
        {
            var distributedCacheForTest =
                new MemoryDistributedCache(new OptionsManager<MemoryDistributedCacheOptions>(
                                            new OptionsFactory<MemoryDistributedCacheOptions>(
                                             new List<IConfigureOptions<MemoryDistributedCacheOptions>>(),
                                             new List<IPostConfigureOptions<MemoryDistributedCacheOptions>
                                             >())));

            _cache = new DistributedTypedCache(distributedCacheForTest,
                                               nameof(DistributedTypedCacheTests),
                                               new DistributedCacheEntryOptions
                                               {
                                                   AbsoluteExpirationRelativeToNow =
                                                       TimeSpan.FromSeconds(2)
                                               });
        }

        [FsCheck.NUnit.Property]
        public void GetOrAddPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new DataToCache("Org", "GetOrAddPropertyTest", DateTimeOffset.UtcNow,
                                             DateTimeOffset.UtcNow);

            //Act
            var entryFromCache = _cache.GetOrAdd($"{nameof(GetOrAddPropertyTest)}{key.Get}", _ => entryToAdd);

            //Assert
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedAt));
        }


        [FsCheck.NUnit.Property]
        public void SetGetPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new DataToCache("Org", "SetGetPropertyTest", DateTimeOffset.UtcNow,
                                             DateTimeOffset.UtcNow);
            var cacheKey = $"{nameof(SetGetPropertyTest)}{key.Get}";

            //Act
            _cache.Set(cacheKey, entryToAdd);
            var entryFromCache = _cache.Get<DataToCache>(cacheKey);

            //Assert
            // ignore the last modification datetime, since the same key could be added twice, and thus the timestamp can differ.
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedAt));
        }

        [FsCheck.NUnit.Property]
        public void SetGetAsyncPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new DataToCache("Org", "SetGetAsyncPropertyTest", DateTimeOffset.UtcNow,
                                             DateTimeOffset.UtcNow);

            var cacheKey = $"{nameof(SetGetAsyncPropertyTest)}{key.Get}";

            //Act
            _cache.SetAsync(cacheKey, entryToAdd).GetAwaiter().GetResult();
            var entryFromCache = _cache.GetAsync<DataToCache>(cacheKey).GetAwaiter().GetResult();

            //Assert
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedAt));
        }

        [Test]
        public void GetOrAddAddsSuccess()
        {
            //Arrange
            var entryToAdd =
                new DataToCache("Org", "GetOrAddAddsSuccess", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            //Act
            var entryFromCache = _cache.GetOrAdd(nameof(GetOrAddAddsSuccess), _ => entryToAdd);

            //Assert
            entryFromCache.Should().BeEquivalentTo(entryToAdd);
        }

        [Test]
        public async Task GetOrAddAddsAsyncSuccess()
        {
            //Arrange
            var entryToAdd = new DataToCache("Org", "GetOrAddAddsAsyncSuccess", DateTimeOffset.UtcNow,
                                             DateTimeOffset.UtcNow);

            //Act
            var entryFromCache =
                await _cache.GetOrAddAsync(nameof(GetOrAddAddsAsyncSuccess), _ => Task.FromResult(entryToAdd));

            //Assert
            entryFromCache.Should().BeEquivalentTo(entryToAdd);
        }

        [Test]
        public void GetOrAddGetsSuccess()
        {
            //Arrange
            int timesCalled = 0;
            var entryToAdd =
                new DataToCache("Org", "GetOrAddGetsSuccess", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            _cache.GetOrAdd(nameof(GetOrAddGetsSuccess),
                            _ =>
                            {
                                timesCalled++;
                                return entryToAdd;
                            });

            //Act
            var entryFromCache = _cache.GetOrAdd(nameof(GetOrAddGetsSuccess),
                                                 _ =>
                                                 {
                                                     timesCalled++;
                                                     return entryToAdd;
                                                 });


            //Assert
            entryFromCache.Should().BeEquivalentTo(entryToAdd);
            timesCalled.Should().Be(1);
        }

        [Test]
        public void GetAndRemoveSuccess()
        {
            //Arrange
            var entryFromCache =
                _cache.GetOrAdd(nameof(GetAndRemoveSuccess),
                                _ => new DataToCache("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            _cache.Get<DataToCache>(nameof(GetAndRemoveSuccess)).Should().BeEquivalentTo(entryFromCache);

            //Act
            _cache.Remove(nameof(GetAndRemoveSuccess));

            //Assert
            _cache.Get<DataToCache>(nameof(GetAndRemoveSuccess)).Should().BeNull();
        }

        [FsCheck.NUnit.Property]
        public void GetAndRemovePropertyTests(NonEmptyString key)
        {
            //Arrange
            var entryFromCache =
                _cache.GetOrAdd(key.Get,
                                _ => new DataToCache("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            _cache.Get<DataToCache>(key.Get).Should().BeEquivalentTo(entryFromCache);

            //Act
            _cache.Remove(key.Get);

            //Assert
            _cache.Get<DataToCache>(key.Get).Should().BeNull();
        }

        [FsCheck.NUnit.Property]
        public void RemoveNonExistentKeysNoErrors(NonEmptyString key)
        {
            //Arrange & Act
            _cache.Remove(key.Get);
        }

        [Test]
        public async Task GetAndRemoveAsyncSuccess()
        {
            //Arrange
            var entryFromCache =
                _cache.GetOrAdd(nameof(GetAndRemoveSuccess),
                                _ => new DataToCache("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            (await _cache.GetAsync<DataToCache>(nameof(GetAndRemoveSuccess))).Should().BeEquivalentTo(entryFromCache);

            //Act
            await _cache.RemoveAsync(nameof(GetAndRemoveSuccess));

            //Assert
            (await _cache.GetAsync<DataToCache>(nameof(GetAndRemoveSuccess))).Should().BeNull();
        }
    }

    public class DataToCache
    {
        public string         Id             { get; }
        public string         Name           { get; }
        public DateTimeOffset CreatedAt      { get; }
        public DateTimeOffset LastModifiedAt { get; }

        public DataToCache(string id, string name, DateTimeOffset createdAt, DateTimeOffset lastModifiedAt)
        {
            Id             = id;
            Name           = name;
            CreatedAt      = createdAt;
            LastModifiedAt = lastModifiedAt;
        }
    }
}
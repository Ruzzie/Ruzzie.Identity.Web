using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Ruzzie.Identity.Storage.Azure.Entities;
using Ruzzie.Identity.Storage.Caching;

namespace Ruzzie.Identity.Storage.UnitTests.Caching
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
                                               new DistributedCacheEntryOptions()
                                               {
                                                   AbsoluteExpirationRelativeToNow =
                                                       TimeSpan.FromSeconds(2)
                                               });
        }

        [FsCheck.NUnit.Property]
        public void GetOrAddPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new Organisation("Org", "GetOrAddPropertyTest", DateTimeOffset.UtcNow,
                                              DateTimeOffset.UtcNow);

            //Act
            var entryFromCache = _cache.GetOrAdd($"{nameof(GetOrAddPropertyTest)}{key.Get}", _ => entryToAdd);

            //Assert
            entryFromCache.Should().BeEquivalentTo(entryToAdd);
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedDateTimeUtc));
        }


        [FsCheck.NUnit.Property]
        public void SetGetPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new Organisation("Org", "SetGetPropertyTest", DateTimeOffset.UtcNow,
                                              DateTimeOffset.UtcNow);
            var cacheKey = $"{nameof(SetGetPropertyTest)}{key.Get}";

            //Act
            _cache.Set(cacheKey, entryToAdd);
            var entryFromCache = _cache.Get<Organisation>(cacheKey);

            //Assert
            // ignore the last modification datetime, since the same key could be added twice, and thus the timestamp can differ.
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedDateTimeUtc));
        }

        [FsCheck.NUnit.Property]
        public void SetGetAsyncPropertyTest(NonEmptyString key)
        {
            //Arrange
            var entryToAdd = new Organisation("Org", "SetGetAsyncPropertyTest", DateTimeOffset.UtcNow,
                                              DateTimeOffset.UtcNow);

            var cacheKey = $"{nameof(SetGetAsyncPropertyTest)}{key.Get}";

            //Act
            _cache.SetAsync(cacheKey, entryToAdd).GetAwaiter().GetResult();
            var entryFromCache = _cache.GetAsync<Organisation>(cacheKey).GetAwaiter().GetResult();

            //Assert
            entryFromCache.Should()
                          .BeEquivalentTo(entryToAdd, options => options.Excluding(x => x.LastModifiedDateTimeUtc));
        }

        [Test]
        public void GetOrAddAddsSuccess()
        {
            //Arrange
            var entryToAdd =
                new Organisation("Org", "GetOrAddAddsSuccess", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            //Act
            var entryFromCache = _cache.GetOrAdd(nameof(GetOrAddAddsSuccess), _ => entryToAdd);

            //Assert
            entryFromCache.Should().BeEquivalentTo(entryToAdd);
        }

        [Test]
        public async Task GetOrAddAddsAsyncSuccess()
        {
            //Arrange
            var entryToAdd = new Organisation("Org", "GetOrAddAddsAsyncSuccess", DateTimeOffset.UtcNow,
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
                new Organisation("Org", "GetOrAddGetsSuccess", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

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
                                _ => new Organisation("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            _cache.Get<Organisation>(nameof(GetAndRemoveSuccess)).Should().BeEquivalentTo(entryFromCache);

            //Act
            _cache.Remove(nameof(GetAndRemoveSuccess));

            //Assert
            _cache.Get<Organisation>(nameof(GetAndRemoveSuccess)).Should().BeNull();
        }

        [FsCheck.NUnit.Property]
        public void GetAndRemovePropertyTests(NonEmptyString key)
        {
            //Arrange
            var entryFromCache =
                _cache.GetOrAdd(key.Get,
                                _ => new Organisation("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            _cache.Get<Organisation>(key.Get).Should().BeEquivalentTo(entryFromCache);

            //Act
            _cache.Remove(key.Get);

            //Assert
            _cache.Get<Organisation>(key.Get).Should().BeNull();
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
                                _ => new Organisation("Org", "TestUser", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            //precondition check
            (await _cache.GetAsync<Organisation>(nameof(GetAndRemoveSuccess))).Should().BeEquivalentTo(entryFromCache);

            //Act
            await _cache.RemoveAsync(nameof(GetAndRemoveSuccess));

            //Assert
            (await _cache.GetAsync<Organisation>(nameof(GetAndRemoveSuccess))).Should().BeNull();
        }
    }
}
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure;

namespace Ruzzie.Identity.Storage.UnitTests.Azure
{
    [Category("TableStorage.IntegrationTests")]
    [TestFixture]
    public class OrganisationRepositoryWithCacheIntegrationTests : OrganisationRepositoryIntegrationTestsBase
    {
        protected override string TestTablesPrefixName { get; } = nameof(OrganisationRepositoryWithCacheIntegrationTests);

        protected override IOrganisationRepository CreateOrgRepository(CloudStorageAccount cloudStorageAccount)
        {
            var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            return new OrganisationRepositoryWithCache(new OrganisationRepository(
                    new CloudTablePool(TestTablesPrefixName,             cloudTableClient),
                    new CloudTablePool(TestTablesPrefixName + "userOrg", cloudTableClient),
                    new CloudTablePool(TestTablesPrefixName + "orgUser", cloudTableClient),
                    new CloudTablePool(TestTablesPrefixName + "Invites", cloudTableClient)
                ),
                new MemoryDistributedCache(new OptionsManager<MemoryDistributedCacheOptions>(
                    new OptionsFactory<MemoryDistributedCacheOptions>(
                        new List<IConfigureOptions<MemoryDistributedCacheOptions>>(),
                        new List<IPostConfigureOptions<MemoryDistributedCacheOptions>>()))));
        }
    }
}
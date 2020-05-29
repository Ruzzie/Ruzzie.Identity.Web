using Microsoft.Azure.Cosmos.Table;
using NUnit.Framework;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure;

namespace Ruzzie.Identity.Storage.UnitTests.Azure
{
    [Category("TableStorage.IntegrationTests")]
    [TestFixture]
    public class OrganisationRepositoryIntegrationTests : OrganisationRepositoryIntegrationTestsBase
    {
        protected override string TestTablesPrefixName { get; } = nameof(OrganisationRepositoryIntegrationTests);

        protected override IOrganisationRepository CreateOrgRepository(CloudStorageAccount cloudStorageAccount)
        {
            return new OrganisationRepository(
                new CloudTablePool(TestTablesPrefixName, cloudStorageAccount),
                new CloudTablePool(TestTablesPrefixName + "userOrg", cloudStorageAccount),
                new CloudTablePool(TestTablesPrefixName + "orgUser", cloudStorageAccount),
                new CloudTablePool(TestTablesPrefixName + "Invites", cloudStorageAccount)
            );
        }
    }
}
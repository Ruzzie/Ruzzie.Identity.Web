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
            var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            return new OrganisationRepository(new CloudTablePool(TestTablesPrefixName,             cloudTableClient),
                                              new CloudTablePool(TestTablesPrefixName + "userOrg", cloudTableClient),
                                              new CloudTablePool(TestTablesPrefixName + "orgUser", cloudTableClient),
                                              new CloudTablePool(TestTablesPrefixName + "Invites", cloudTableClient));
        }
    }
}
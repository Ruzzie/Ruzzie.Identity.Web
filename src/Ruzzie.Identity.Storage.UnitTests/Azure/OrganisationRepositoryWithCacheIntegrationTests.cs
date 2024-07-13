using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.UnitTests.Azure;

[Category("TableStorage.IntegrationTests")]
[TestFixture]
public class OrganisationRepositoryWithCacheIntegrationTests : OrganisationRepositoryIntegrationTestsBase
{
    private CloudTablePool _organisationTable;

    protected override string TestTablesPrefixName { get; } =
        nameof(OrganisationRepositoryWithCacheIntegrationTests);

    protected override IOrganisationRepository CreateOrgRepository(CloudStorageAccount cloudStorageAccount)
    {
        var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
        _organisationTable = new CloudTablePool(TestTablesPrefixName, cloudTableClient);
        return new OrganisationRepositoryWithCache(new OrganisationRepository(
                                                                              _organisationTable
                                                                            , new CloudTablePool(TestTablesPrefixName +
                                                                                                 "userOrg"
                                                                                               , cloudTableClient)
                                                                            , new CloudTablePool(TestTablesPrefixName +
                                                                                                 "orgUser"
                                                                                               , cloudTableClient)
                                                                            , new CloudTablePool(TestTablesPrefixName +
                                                                                                 "Invites"
                                                                                               , cloudTableClient)
                                                                             )
                                                 , new MemoryDistributedCache(new OptionsManager<
                                                                                  MemoryDistributedCacheOptions>(
                                                                                                                 new
                                                                                                                     OptionsFactory
                                                                                                                     <MemoryDistributedCacheOptions>(
                                                                                                                                                     new
                                                                                                                                                         List
                                                                                                                                                         <IConfigureOptions
                                                                                                                                                             <
                                                                                                                                                                 MemoryDistributedCacheOptions>>()
                                                                                                                                                   , new
                                                                                                                                                         List
                                                                                                                                                         <IPostConfigureOptions
                                                                                                                                                             <
                                                                                                                                                                 MemoryDistributedCacheOptions>>()))));
    }


    [Test]
    public void OldOrgDataShouldDeserialize_CorrectFromCache_Test()
    {
        //Arrange
        var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(DeleteOrganisation_SmokeTest));
        var orgName =
            CreateUniqueOrganisationNameForTest(nameof(OldOrgDataShouldDeserialize_CorrectFromCache_Test));

        var org = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);


        //Execute a legacy entity: has no createdBy, lastModified, etc. fields

        var dynamicTableEntity = new DynamicTableEntity(org.PartitionKey, org.RowKey);
        dynamicTableEntity["CompanyName"] = EntityProperty.GeneratePropertyForString(org.CompanyName);

        _organisationTable.Table.ExecuteAsync(TableOperation.Insert(dynamicTableEntity))
                                 .GetAwaiter()
                                 .GetResult();


        Repository.AddUserToOrganisation(userId, org.RowKey, "Default", DateTimeOffset.UtcNow);

        //Act & Assert
        Repository.GetOrganisationById(org.RowKey).Should().NotBeNull();

        //Second time should also succeed, verify that when it is read from cache everything is OK
        Repository.GetOrganisationById(org.RowKey).Should().NotBeNull();
    }
}
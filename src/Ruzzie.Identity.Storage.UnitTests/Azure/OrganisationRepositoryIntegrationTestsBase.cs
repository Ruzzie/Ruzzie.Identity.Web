using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.UnitTests.Azure
{
    public abstract class OrganisationRepositoryIntegrationTestsBase
    {
        protected IOrganisationRepository Repository { get; set; }
        protected abstract string TestTablesPrefixName { get; }
        protected abstract IOrganisationRepository CreateOrgRepository(CloudStorageAccount cloudStorageAccount);

        protected static string CreateUniqueOrganisationNameForTest(string testName)
        {
            var email = ("-" + testName + $"_{Guid.NewGuid()}" + "ACME INC.")
                .ToLowerInvariant();
            return email;
        }

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            var connString = config.GetConnectionString("AzureStorage");

            var cloudStorageAccount = CloudStorageAccount.Parse(connString);
            Repository = CreateOrgRepository(cloudStorageAccount);
        }

        [Test]
        public void OrgDoesntExistsSmokeTest()
        {
            //Act & Assert
            Repository.OrganisationExists(CreateUniqueOrganisationNameForTest(nameof(OrgDoesntExistsSmokeTest))).Should().BeFalse();
        }

        [Test]
        public void InsertOrgAndCheckIfExists()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(InsertOrgAndCheckIfExists));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(InsertOrgAndCheckIfExists));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            Repository.OrganisationExists(orgName).Should().BeFalse();

            //Act
            var insertedEntity = Repository.InsertNewOrganisation(entityToInsert);

            //Assert
            Repository.OrganisationExists(orgName).Should().BeTrue();
            insertedEntity.Should().BeEquivalentTo(entityToInsert,
                options => options.IncludingAllDeclaredProperties().Excluding(r => r.Timestamp).Excluding(r => r.ETag));
        }

        [Test]
        public void GetOrgByIdThatExistsSmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(GetOrgByIdThatExistsSmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(GetOrgByIdThatExistsSmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var insertedEntity = Repository.InsertNewOrganisation(entityToInsert);

            //Act
            var gotEntity = Repository.GetOrganisationById(orgName.CreateAlphaNumericKey(
                KeyGenerators.AlphaNumericKeyGenOptions.TrimInput |
                KeyGenerators.AlphaNumericKeyGenOptions.PreserveSpacesAsDashes));

            //Assert
            gotEntity.Should().BeEquivalentTo(insertedEntity,
                options => options.Excluding(x => x.SelectedMemberInfo.DeclaringType == typeof(TableEntity)));
        }

        [Test]
        public void GetOrgByIdThatNotExistsSmokeTest()
        {
            //Arrange
            var orgName = CreateUniqueOrganisationNameForTest(nameof(InsertOrgAndCheckIfExists));

            //Act
            var gotEntity = Repository.GetOrganisationById(orgName.CreateAlphaNumericKey(
                KeyGenerators.AlphaNumericKeyGenOptions.TrimInput |
                KeyGenerators.AlphaNumericKeyGenOptions.PreserveSpacesAsDashes));

            //Assert
            gotEntity.Should().BeNull();
        }

        [Test]
        public void UpdateOrgSmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(UpdateOrgSmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(UpdateOrgSmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var entity = Repository.InsertNewOrganisation(entityToInsert);
            var originalTimeStamp = entity.LastModifiedDateTimeUtc;
            entity.CreatedByUserId = "harry@UpdateOrgSmokeTest.org";

            Thread.Sleep(1);

            //Act
            var updatedEntity = Repository.UpdateOrganisation(entity);

            //Assert
            //ReferenceEquals(entity, updatedEntity).Should().BeFalse();

            updatedEntity.Should().BeSameAs(entity);//GRRR: It returns the same reference: this is behavior of the Table Storage SDK.
            updatedEntity.LastModifiedDateTimeUtc.Should().BeAfter(originalTimeStamp);
        }

        [Test]
        public void AddUserToOrgSmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(UpdateOrgSmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(UpdateOrgSmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var entity = Repository.InsertNewOrganisation(entityToInsert);

            //Act
            Repository.AddUserToOrganisation(userId, entity.RowKey, "Default", DateTimeOffset.UtcNow);

            //Assert
            Repository.GetOrganisationsForUser(userId).Should().HaveCount(1);
            Repository.GetUsersForOrganisation(entity.RowKey).Should().HaveCount(1);
        }

        [Test]
        public void UserIsInOrganisationSmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(UserIsInOrganisationSmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(UserIsInOrganisationSmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var entity = Repository.InsertNewOrganisation(entityToInsert);

            //Act & Assert 1
            Repository.UserIsInOrganisation(entity.RowKey, userId).Should().BeFalse();

            //Arrange for 2
            Repository.AddUserToOrganisation(userId, entity.RowKey, "Default", DateTimeOffset.UtcNow);

            //Act & Assert 2
            Repository.UserIsInOrganisation(entity.RowKey, userId).Should().BeTrue();
        }

        [Test]
        public void UpsertOrganisationInvite_Insert_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(UpsertOrganisationInvite_Insert_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(UpsertOrganisationInvite_Insert_SmokeTest));
            var organisation = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            var entityToInsert = new OrganisationInvite(organisation.RowKey, userId, "valid@valid.org",
                "myInvitationtoken", DateTimeOffset.UtcNow, 0);

            //Act
            var entity = Repository.UpsertOrganisationInvite(entityToInsert);

            //Assert
            entityToInsert.Should().BeSameAs(entity);//GRRR: It returns the same reference: this is behavior of the Table Storage SDK.
        }

        [Test]
        public void UpsertOrganisationInvite_Update_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(UpsertOrganisationInvite_Update_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(UpsertOrganisationInvite_Update_SmokeTest));
            var originalModificationDateTime = DateTimeOffset.UtcNow;
            var organisation = new Organisation(orgName, userId, DateTimeOffset.UtcNow, originalModificationDateTime);
            var entityToInsert = new OrganisationInvite(organisation.RowKey, userId, "valid@valid.org",
                "myInvitationtoken", DateTimeOffset.UtcNow, 0);

            //Insert
            var entity = Repository.UpsertOrganisationInvite(entityToInsert);
            Thread.Sleep(1);
            entity.InvitationToken = "UpdatedToken";
            entity.LastModifiedDateTimeUtc = DateTimeOffset.UtcNow;

            //Act
            var updatedEntity = Repository.UpsertOrganisationInvite(entity);

            //Assert
            updatedEntity.InvitationToken.Should().Be("UpdatedToken");
            entityToInsert.Should().BeSameAs(entity);//GRRR: It returns the same reference: this is behavior of the Table Storage SDK.
            updatedEntity.LastModifiedDateTimeUtc.Should().BeAfter(originalModificationDateTime);
        }

        [Test]
        public void GetOrganisationInvite_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(GetOrganisationInvite_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(GetOrganisationInvite_SmokeTest));
            var originalModificationDateTime = DateTimeOffset.UtcNow;
            var organisation = new Organisation(orgName, userId, DateTimeOffset.UtcNow, originalModificationDateTime);
            var entityToInsert = new OrganisationInvite(organisation.RowKey, userId, "valid@valid.org",
                "myInvitationtoken", DateTimeOffset.UtcNow, 0);

            //Insert
            var entity = Repository.UpsertOrganisationInvite(entityToInsert);

            //Act
            var retrievedEntity = Repository.GetOrganisationInvite(entity.PartitionKey, userId);

            //Act
            entity.Should().BeEquivalentTo(retrievedEntity,
                options => options.IncludingAllDeclaredProperties()
                    .Excluding(r => r.Timestamp)
                    .Excluding(r => r.ETag)
                    .Excluding(x => x.SelectedMemberInfo.DeclaringType == typeof(TableEntity)));
        }

        [Test]
        public void GetAllOrganisationInvites_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(GetAllOrganisationInvites_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(GetAllOrganisationInvites_SmokeTest));
            var originalModificationDateTime = DateTimeOffset.UtcNow;
            var organisation = new Organisation(orgName, userId, DateTimeOffset.UtcNow, originalModificationDateTime);
            var entityToInsert = new OrganisationInvite(organisation.RowKey, userId, "valid@valid.org",
                "myInvitationtoken", DateTimeOffset.UtcNow, 0);

            //Insert
            var entity = Repository.UpsertOrganisationInvite(entityToInsert);

            //Act
            var invites = Repository.GetAllOrganisationInvites(entity.OrganisationId ?? "error", 0);

            //Assert
            invites.Should().HaveCount(1);
        }

        [Test]
        public void DeleteUserFromOrganisation_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(DeleteUserFromOrganisation_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(DeleteUserFromOrganisation_SmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var entity = Repository.InsertNewOrganisation(entityToInsert);

            Repository.AddUserToOrganisation(userId, entity.RowKey, "Default", DateTimeOffset.UtcNow);

            //Act
            Repository.DeleteUserFromOrganisation(userId, entity.RowKey);

            //Assert
            Repository.UserIsInOrganisation(entity.RowKey, userId).Should().BeFalse();
        }

        [Test]
        public void DeleteOrganisationInvite_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(DeleteOrganisationInvite_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(DeleteOrganisationInvite_SmokeTest));
            var originalModificationDateTime = DateTimeOffset.UtcNow;
            var organisation = new Organisation(orgName, userId, DateTimeOffset.UtcNow, originalModificationDateTime);
            var entityToInsert = new OrganisationInvite(organisation.RowKey, userId, "valid@valid.org",
                "myInvitationtoken", DateTimeOffset.UtcNow, 0);

            //Insert
            var entity = Repository.UpsertOrganisationInvite(entityToInsert);

            //Act
            Repository.DeleteOrganisationInvite(entity.PartitionKey, entity.RowKey);

            //Assert
            Repository.GetAllOrganisationInvites(entity.PartitionKey, 0).Should().HaveCount(0);
        }

        [Test]
        public void DeleteOrganisation_SmokeTest()
        {
            //Arrange
            var userId = UserRepositoryIntegrationTests.CreateUniqueEmailForTest(nameof(DeleteOrganisation_SmokeTest));
            var orgName = CreateUniqueOrganisationNameForTest(nameof(DeleteOrganisation_SmokeTest));
            var entityToInsert = new Organisation(orgName, userId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var entity = Repository.InsertNewOrganisation(entityToInsert);
            Repository.AddUserToOrganisation(userId, entity.RowKey, "Default", DateTimeOffset.UtcNow);

            //Act
            Repository.DeleteOrganisation(entity.RowKey);

            //Assert
            Repository.GetOrganisationsForUser(userId).Should().HaveCount(0);
            Repository.GetUsersForOrganisation(entity.RowKey).Should().HaveCount(0);
        }

        [Test]
        public void GetAllOrganisationIds_SmokeTest()
        {
            Repository.GetAllOrganisationIds().Should().HaveCountGreaterThan(0);
        }


    }
}
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
    [Category("TableStorage.IntegrationTests")]
    public class UserRepositoryIntegrationTests
    {
        private UserRepository _repository;
        private string _testTableName;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();

            var connString = config.GetConnectionString("AzureStorage");

            _testTableName = nameof(UserRepositoryIntegrationTests);
            _repository =
                new UserRepository(new CloudTablePool(_testTableName,
                                                      CloudStorageAccount.Parse(connString).CreateCloudTableClient()));
        }

        public static string CreateUniqueEmailForTest(string testName)
        {
            var email = ("-" + testName + $"_{Guid.NewGuid()}" + "@test.pvhlink.com")
                .ToLowerInvariant();
            return email;
        }

        [Test]
        public void UserDoesntExistsSmokeTest()
        {
            //Act & Assert
            _repository.UserExists(CreateUniqueEmailForTest(nameof(UserDoesntExistsSmokeTest))).Should().BeFalse();
        }

        [Test]
        public void InsertRegistrationAndCheckIfExists()
        {
            //Arrange
            var email = CreateUniqueEmailForTest(nameof(InsertRegistrationAndCheckIfExists));
            var entityToInsert = new UserRegistration(email, "nopwd", "Repository", "Test", "notoken", DateTimeOffset.UtcNow);

            _repository.UserExists(email).Should().BeFalse();

            //Act
            var insertedEntity = _repository.InsertNewUser(entityToInsert);

            //Assert
            _repository.UserExists(email).Should().BeTrue();
            insertedEntity.Should().BeEquivalentTo(entityToInsert,
                options => options.IncludingAllDeclaredProperties().Excluding(r => r.Timestamp).Excluding(r => r.ETag));
        }

        [Test]
        public void GetUserByEmailThatExistsSmokeTest()
        {
            //Arrange
            var email = CreateUniqueEmailForTest(nameof(GetUserByEmailThatExistsSmokeTest));
            var entityToInsert = new UserRegistration(email, "nopwd", "Repository", "Test", "notoken", DateTimeOffset.UtcNow);
            var insertedEntity = _repository.InsertNewUser(entityToInsert);

            //Act
            var gotEntity = _repository.GetUserByEmail(email);

            //Assert
            gotEntity.Should().BeEquivalentTo(insertedEntity,
                options => options.Excluding(x => x.SelectedMemberInfo.DeclaringType == typeof(TableEntity)));
        }

        [Test]
        public void GetUserByEmailThatNotExistsSmokeTest()
        {
            //Arrange
            var email = CreateUniqueEmailForTest(nameof(GetUserByEmailThatNotExistsSmokeTest));

            //Act
            var gotEntity = _repository.GetUserByEmail(email);

            //Assert
            gotEntity.Should().BeNull();
        }

        [Test]
        public void UpdateUserSmokeTest()
        {
            //Arrange
            var email = CreateUniqueEmailForTest(nameof(GetUserByEmailThatExistsSmokeTest));
            var entityToInsert = new UserRegistration(email, "nopwd", "Repository", "Test", "notoken", DateTimeOffset.UtcNow);
            var entity = _repository.InsertNewUser(entityToInsert);
            var originalTimeStamp = entity.LastModifiedDateTimeUtc;

            entity.ValidationStatusUpdateDateTimeUtc = DateTimeOffset.UtcNow;
            entity.Firstname = nameof(UpdateUserSmokeTest);
            Thread.Sleep(1);

            //Act
            var updatedEntity = _repository.UpdateUser(entity);

            //Assert
            //ReferenceEquals(entity, updatedEntity).Should().BeFalse();

            updatedEntity.Should().BeSameAs(entity);//GRRR: It returns the same reference: this is behavior of the Table Storage SDK.
            // updatedEntity.Should().BeEquivalentTo(entity,
            //     options => options.IncludingAllDeclaredProperties().Excluding(r => r.Timestamp).Excluding(r => r.ETag));
            updatedEntity.LastModifiedDateTimeUtc.Should().BeAfter(originalTimeStamp);
        }

        [Test]
        public void DeleteUserSmokeTest()
        {
            //Arrange
            var email = CreateUniqueEmailForTest(nameof(GetUserByEmailThatExistsSmokeTest));
            var entityToInsert = new UserRegistration(email, "nopwd", "Repository", "Test", "notoken", DateTimeOffset.UtcNow);

            //Insert
            var _ = _repository.InsertNewUser(entityToInsert);

            //Act
            _repository.DeleteUser(email);

            //Assert
            _repository.UserExists(email).Should().BeFalse();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MessagePack.Resolvers;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching.Distributed;
using Ruzzie.Azure.Storage;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.Azure
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private readonly CloudTablePool _organisationTable;
        private readonly CloudTablePool _userOrganisationTable;
        private readonly CloudTablePool _organisationUserTable;
        private readonly CloudTablePool _organisationInvitesTable;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public OrganisationRepository(
            CloudTablePool organisationTable,
            CloudTablePool user_organisation_Table,
            CloudTablePool organisation_user_Table,
            CloudTablePool organisationInvitesTable
            )
        {
            _organisationTable = organisationTable;
            _userOrganisationTable = user_organisation_Table;
            _organisationUserTable = organisation_user_Table;
            _organisationInvitesTable = organisationInvitesTable;
        }

        public bool OrganisationExists(string organisationName)
        {
            if (string.IsNullOrWhiteSpace(organisationName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationName));
            }

            var partitionKey = string.Empty;
            var rowKey = string.Empty;

            try
            {
                rowKey = organisationName.CreateAlphaNumericKey(
                    KeyGenerators.AlphaNumericKeyGenOptions.TrimInput |
                    KeyGenerators.AlphaNumericKeyGenOptions.PreserveSpacesAsDashes);

                partitionKey = rowKey.CalculatePartitionKeyForAlphaNumericRowKey();

                return _organisationTable.RowExistsForPartitionKeyAndRowKey(partitionKey, rowKey);
            }
            catch (Exception e)
            {
                throw new Exception($"Error checking {nameof(OrganisationExists)} in [{_organisationTable.TableName}] with [{organisationName}]: [{partitionKey}]-[{rowKey}]. [{e.Message}]", e);
            }
        }

        public Organisation InsertNewOrganisation(Organisation entity)
        {
            if (ReferenceEquals(entity, null))
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                return _organisationTable.InsertEntity(entity);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(InsertNewOrganisation)}] in [{_organisationTable.TableName}]: [{entity.PartitionKey}]-[{entity.RowKey}]. [{e.Message}]",
                    e);
            }
        }

        public void AddUserToOrganisation(string userId, string organisationId, string role, DateTimeOffset joinedCreationDateTimeUtc)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(role));
            }

            var partitionKey = string.Empty;
            var rowKey = string.Empty;

            //1: Add to user_organisation table
            try
            {
                partitionKey = userId;
                rowKey = organisationId;
                var userOrgEntity = new UserOrganisation(partitionKey, rowKey, role, joinedCreationDateTimeUtc);

                _userOrganisationTable.InsertEntity(userOrgEntity);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(AddUserToOrganisation)}] in [{_userOrganisationTable.TableName}]: [{partitionKey}]-[{rowKey}]. [{e.Message}]",
                    e);
            }

            partitionKey = string.Empty;
            rowKey = string.Empty;
            //2: Add to organisation_user table
            try
            {
                partitionKey = organisationId;
                rowKey = userId;
                var orgUserEntity = new OrganisationUser( partitionKey, rowKey, role, joinedCreationDateTimeUtc);

                _organisationUserTable.InsertEntity(orgUserEntity);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(AddUserToOrganisation)}] in [{_organisationUserTable.TableName}]: [{partitionKey}]-[{rowKey}]. [{e.Message}]",
                    e);
            }
        }

        public void DeleteUserFromOrganisation(string userId, string organisationId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            var partitionKey = string.Empty;
            var rowKey = string.Empty;

            //1: Delete from user_org
            try
            {
                partitionKey = userId;
                rowKey = organisationId;
                _userOrganisationTable.Delete(partitionKey, rowKey);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(DeleteUserFromOrganisation)}] in [{_userOrganisationTable.TableName}]: [{partitionKey}]-[{rowKey}]. [{e.Message}]",
                    e);
            }
            finally
            {
                //2: Delete from org_user
                try
                {
                    partitionKey = organisationId;
                    rowKey = userId;
                    _organisationUserTable.Delete(partitionKey, rowKey);
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Error for [{nameof(DeleteUserFromOrganisation)}] in [{_organisationUserTable.TableName}]: [{partitionKey}]-[{rowKey}]. [{e.Message}]",
                        e);
                }
            }
            //?: Delete org when org has no users, nope not for now

            //?: What if the user was the user that created the organisation....: do nothing for now
        }

        public Organisation GetOrganisationById(string organisationId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            var partitionKey = string.Empty;

            try
            {
                partitionKey = organisationId.CalculatePartitionKeyForAlphaNumericRowKey();
                return _organisationTable.GetEntity<Organisation>(partitionKey, organisationId);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(GetOrganisationById)}] in [{_organisationTable.TableName}] with [{organisationId}]: [{partitionKey}]-[{organisationId}]. [{e.Message}]", e);
            }
        }

        public void DeleteOrganisation(string organisationId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            var partitionKey = string.Empty;

            try
            {
                partitionKey = organisationId.CalculatePartitionKeyForAlphaNumericRowKey();
                _organisationTable.Delete(partitionKey, organisationId);

                var usersForOrganisation = GetUsersForOrganisation(organisationId);
                if (usersForOrganisation?.Count > 0)
                {
                    for (var i = 0; i < usersForOrganisation.Count; i++)
                    {
                        var organisationUser = usersForOrganisation[i];
                        DeleteUserFromOrganisation(organisationUser.RowKey, organisationUser.PartitionKey);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(DeleteOrganisation)}] in [{_organisationTable.TableName}] with [{organisationId}]: [{partitionKey}]-[{organisationId}]. [{e.Message}]", e);
            }
        }

        public bool UserIsInOrganisation(string organisationId, string userId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            var partitionKey = userId;
            var rowKey = organisationId;
            try
            {
                return _userOrganisationTable.RowExistsForPartitionKeyAndRowKey(partitionKey, rowKey);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(UserIsInOrganisation)}] in [{_userOrganisationTable.TableName}] with [{organisationId}]: [{partitionKey}]-[{organisationId}]. [{e.Message}]", e);
            }
        }

        public IReadOnlyList<UserOrganisation> GetOrganisationsForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            var partitionKey = userId;

            try
            {
                return _userOrganisationTable.GetAllEntitiesInPartition<UserOrganisation>(userId);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(GetOrganisationsForUser)}] in [{_userOrganisationTable.TableName}] with [{userId}]: [{partitionKey}]-[*]. [{e.Message}]", e);
            }
        }

        public IReadOnlyList<OrganisationUser> GetUsersForOrganisation(string organisationId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            var partitionKey = organisationId;

            try
            {
                return _organisationUserTable.GetAllEntitiesInPartition<OrganisationUser>(organisationId);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(GetUsersForOrganisation)}] in [{_organisationUserTable.TableName}] with [{organisationId}]: [{partitionKey}]-[*]. [{e.Message}]", e);
            }
        }

        public Organisation UpdateOrganisation(Organisation entity, DateTimeOffset? utcNow = default)
        {
            if (ReferenceEquals(entity, null))
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                entity.LastModifiedDateTimeUtc = utcNow ?? DateTimeOffset.UtcNow;
                return _organisationTable.UpdateEntity(entity);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(UpdateOrganisation)}] in [{_organisationTable.TableName}]: [{entity.PartitionKey}]-[{entity.RowKey}]. [{e.Message}]",
                    e);
            }
        }

        public OrganisationInvite UpsertOrganisationInvite(OrganisationInvite entity, DateTimeOffset? utcNow = default)
        {
            if (ReferenceEquals(entity, null))
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                entity.LastModifiedDateTimeUtc = utcNow ?? DateTimeOffset.UtcNow;
                return _organisationInvitesTable.InsertOrMergeEntity(entity);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error for [{nameof(UpsertOrganisationInvite)}] in [{_organisationInvitesTable.TableName}]: [{entity.PartitionKey}]-[{entity.RowKey}]. [{e.Message}]",
                    e);
            }
        }

        public OrganisationInvite GetOrganisationInvite(string organisationId, string userId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            try
            {
                return _organisationInvitesTable.GetEntity<OrganisationInvite>(organisationId, userId);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(GetOrganisationInvite)}] in [{_organisationInvitesTable.TableName}] with: [{organisationId}]-[{userId}]. [{e.Message}]", e);
            }
        }

        public void DeleteOrganisationInvite(string organisationId, string userId)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            try
            {
                _organisationInvitesTable.Delete(organisationId, userId);
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(DeleteOrganisationInvite)}] in [{_organisationInvitesTable.TableName}] with: [{organisationId}]-[{userId}]. [{e.Message}]", e);
            }
        }

        public IReadOnlyList<OrganisationInvite> GetAllOrganisationInvites(string organisationId, int invitationStatus)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            try
            {
                return _organisationInvitesTable.Pool.ExecuteOnAvailableObject(table =>
                    {
                        var partitionKeyFilter = TableQuery.GenerateFilterCondition(TableQueryHelpers.PartitionKeyField,
                            TableQueryHelpers.OpEquals, organisationId);
                        var invitationStatusFilter = TableQuery.GenerateFilterConditionForInt(
                            nameof(OrganisationInvite.InvitationStatus), TableQueryHelpers.OpEquals, invitationStatus);

                        var query = new TableQuery<OrganisationInvite>
                        {
                            FilterString = TableQuery.CombineFilters(partitionKeyFilter, TableQueryHelpers.OpAnd,
                                invitationStatusFilter)
                        };

                        return table.ExecuteQuery(query).ToList();
                    }
                );
            }
            catch (Exception e)
            {
                throw new Exception($"Error for [{nameof(GetAllOrganisationInvites)}] in [{_organisationInvitesTable.TableName}] with: [{organisationId}]-[{invitationStatus}]. [{e.Message}]", e);
            }
        }
    }

    public class OrganisationRepositoryWithCache : IOrganisationRepository
    {
        private readonly IOrganisationRepository _orgRepository;
        private readonly IDistributedCache _cache;



        public OrganisationRepositoryWithCache(IOrganisationRepository orgRepository, IDistributedCache cache)
        {
            if (ReferenceEquals(orgRepository, null))
            {
                throw new ArgumentNullException(nameof(orgRepository));
            }

            if (ReferenceEquals(cache, null))
            {
                throw new ArgumentNullException(nameof(cache));
            }

            _orgRepository = orgRepository;
            _cache = cache;
        }

        private const string CacheKeyOrganisationEntityPrefix = "ORG-ENTITY_";
        private const string CacheKeyUserInOrganisationPrefix = "ORG-USER-B_";

        public bool OrganisationExists(string organisationName)
        {
            //no cache needed for now, since this is called sparsly
            return _orgRepository.OrganisationExists(organisationName);
        }

        public Organisation InsertNewOrganisation(Organisation entity)
        {
            var newOrganisation = _orgRepository.InsertNewOrganisation(entity);
            var data = MessagePack.MessagePackSerializer.Serialize(newOrganisation, ContractlessStandardResolver.Options);

            _cache.Set($"{CacheKeyOrganisationEntityPrefix}{newOrganisation.RowKey}", data);

            return newOrganisation;
        }

        public void DeleteOrganisation(string organisationId)
        {
            _orgRepository.DeleteOrganisation(organisationId);
            _cache.Remove($"{CacheKeyOrganisationEntityPrefix}{organisationId}");
        }

        public void AddUserToOrganisation(string userId, string organisationId, string role, DateTimeOffset joinedCreationDateTimeUtc)
        {
            _orgRepository.AddUserToOrganisation(userId, organisationId, role, joinedCreationDateTimeUtc);

            var cacheKey = $"{CacheKeyUserInOrganisationPrefix}{organisationId}{userId}";
            _cache.Set(cacheKey, new []{Convert.ToByte(true)});
        }

        public void DeleteUserFromOrganisation(string userId, string organisationId)
        {
            _orgRepository.DeleteUserFromOrganisation(userId, organisationId);

            var cacheKey = $"{CacheKeyUserInOrganisationPrefix}{organisationId}{userId}";
            _cache.Set(cacheKey, new []{Convert.ToByte(false)});
        }

        public Organisation GetOrganisationById(string organisationId)
        {
            var dataFromCache = _cache.Get($"{CacheKeyOrganisationEntityPrefix}{organisationId}");
            if (dataFromCache != null)
            {
                return MessagePack.MessagePackSerializer.Deserialize<Organisation>(dataFromCache,
                    ContractlessStandardResolver.Options);
            }

            var orgEntity = _orgRepository.GetOrganisationById(organisationId);
            if (orgEntity != default)
            {
                var data = MessagePack.MessagePackSerializer.Serialize(orgEntity, ContractlessStandardResolver.Options);
                _cache.Set($"{CacheKeyOrganisationEntityPrefix}{orgEntity.RowKey}", data);
            }
            return orgEntity;
        }

        public Organisation UpdateOrganisation(Organisation entity, DateTimeOffset? utcNow = default)
        {
            var updateOrganisation = _orgRepository.UpdateOrganisation(entity, utcNow);
            if (updateOrganisation != default)
            {
                var data = MessagePack.MessagePackSerializer.Serialize(updateOrganisation, ContractlessStandardResolver.Options);
                _cache.Set($"{CacheKeyOrganisationEntityPrefix}{updateOrganisation.RowKey}", data);
            }

            return updateOrganisation;
        }

        public bool UserIsInOrganisation(string organisationId, string userId)
        {
            var cacheKey = $"{CacheKeyUserInOrganisationPrefix}{organisationId}{userId}";
            var cachedValue = _cache.Get(cacheKey);
            if (cachedValue != null)
            {
                return Convert.ToBoolean(cachedValue[0]);
            }

            var userIsInOrganisation = _orgRepository.UserIsInOrganisation(organisationId, userId);
            _cache.Set(cacheKey, new[] {Convert.ToByte(userIsInOrganisation)});
            return userIsInOrganisation;
        }

        public IReadOnlyList<UserOrganisation> GetOrganisationsForUser(string userId)
        {
            return _orgRepository.GetOrganisationsForUser(userId);
        }

        public IReadOnlyList<OrganisationUser> GetUsersForOrganisation(string organisationId)
        {
            return _orgRepository.GetUsersForOrganisation(organisationId);
        }

        public OrganisationInvite UpsertOrganisationInvite(OrganisationInvite entity, DateTimeOffset? utcNow = default)
        {
            return _orgRepository.UpsertOrganisationInvite(entity, utcNow);
        }

        public OrganisationInvite GetOrganisationInvite(string organisationId, string userId)
        {
            return _orgRepository.GetOrganisationInvite(organisationId, userId);
        }

        public void DeleteOrganisationInvite(string organisationId, string userId)
        {
            _orgRepository.DeleteOrganisationInvite(organisationId, userId);
        }

        public IReadOnlyList<OrganisationInvite> GetAllOrganisationInvites(string organisationId, int invitationStatus)
        {
            return _orgRepository.GetAllOrganisationInvites(organisationId, invitationStatus);
        }
    }
}
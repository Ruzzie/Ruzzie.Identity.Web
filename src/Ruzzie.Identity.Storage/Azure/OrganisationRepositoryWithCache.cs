using System;
using System.Collections.Generic;
using MessagePack.Resolvers;
using Microsoft.Extensions.Caching.Distributed;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.Azure;

public class OrganisationRepositoryWithCache : IOrganisationRepository
{
    private readonly IOrganisationRepository _orgRepository;
    private readonly IDistributedCache       _cache;

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
        _cache         = cache;
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
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (orgEntity != null)
        {
            var data = MessagePack.MessagePackSerializer.Serialize(orgEntity, ContractlessStandardResolver.Options);
            _cache.Set($"{CacheKeyOrganisationEntityPrefix}{orgEntity.RowKey}", data);
        }
        return orgEntity!;
    }

    public Organisation UpdateOrganisation(Organisation entity, DateTimeOffset? utcNow = default)
    {
        var updateOrganisation = _orgRepository.UpdateOrganisation(entity, utcNow);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (updateOrganisation != null)
        {
            var data = MessagePack.MessagePackSerializer.Serialize(updateOrganisation, ContractlessStandardResolver.Options);
            _cache.Set($"{CacheKeyOrganisationEntityPrefix}{updateOrganisation.RowKey}", data);
        }

        return updateOrganisation!;
    }

    public bool UserIsInOrganisation(string organisationId, string userId)
    {
        var cacheKey    = $"{CacheKeyUserInOrganisationPrefix}{organisationId}{userId}";
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

    public IReadOnlyList<string> GetAllOrganisationIds()
    {
        return _orgRepository.GetAllOrganisationIds();
    }
}
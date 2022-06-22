using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.Azure;

public interface IOrganisationRepository : IOrganisationRepository<Organisation, UserOrganisation, OrganisationUser, OrganisationInvite>
{
    // bool OrganisationExists(string organisationName);
    // Organisation InsertNewOrganisation(Organisation entity);
    // void DeleteOrganisation(string organisationId);
    // void AddUserToOrganisation(string userId, string organisationId, string role, DateTimeOffset joinedCreationDateTimeUtc);
    // void DeleteUserFromOrganisation(string userId, string organisationId);
    // Organisation GetOrganisationById(string organisationId);
    // bool UserIsInOrganisation(string organisationId, string userId);
    // ReadOnlyCollection<UserOrganisation> GetOrganisationsForUser(string userId);
    // ReadOnlyCollection<OrganisationUser> GetUsersForOrganisation(string organisationId);
    //Organisation UpdateOrganisation(Organisation entity, DateTimeOffset? utcNow = default);

    // OrganisationInvite UpsertOrganisationInvite(OrganisationInvite entity, DateTimeOffset? utcNow = default);
    // OrganisationInvite GetOrganisationInvite(string organisationId, string userId);
    // void DeleteOrganisationInvite(string organisationId, string userId);
    // IReadOnlyList<OrganisationInvite> GetAllOrganisationInvites(string organisationId, int invitationStatus);
}
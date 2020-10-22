using System;
using System.Collections.Generic;

namespace Ruzzie.Identity.Storage
{
    public interface IOrganisationRepository<TOrganisation, out TUserOrg, out TOrgUser, TOrgInvite> where TUserOrg: IUserOrgProps where TOrgUser: IUserOrgProps
    {
        bool OrganisationExists(string organisationName);
        TOrganisation InsertNewOrganisation(TOrganisation entity);
        void DeleteOrganisation(string organisationId);
        void AddUserToOrganisation(string userId, string organisationId, string role, DateTimeOffset joinedCreationDateTimeUtc);
        void DeleteUserFromOrganisation(string userId, string organisationId);
        TOrganisation GetOrganisationById(string organisationId);
        TOrganisation UpdateOrganisation(TOrganisation entity, DateTimeOffset? utcNow = default);
        bool UserIsInOrganisation(string organisationId, string userId);
        IReadOnlyList<TUserOrg> GetOrganisationsForUser(string userId);
        IReadOnlyList<TOrgUser> GetUsersForOrganisation(string organisationId);
        TOrgInvite UpsertOrganisationInvite(TOrgInvite entity, DateTimeOffset? utcNow = default);
        TOrgInvite GetOrganisationInvite(string organisationId, string userId);
        void DeleteOrganisationInvite(string organisationId, string userId);
        IReadOnlyList<TOrgInvite> GetAllOrganisationInvites(string organisationId, int invitationStatus);

        IReadOnlyList<string> GetAllOrganisationIds();
    }
}
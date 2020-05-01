using System;
using Microsoft.Azure.Cosmos.Table;

namespace Ruzzie.Identity.Storage.Azure.Entities
{
    public class OrganisationInvite : TableEntity
    {
        public DateTimeOffset CreationDateTimeUtc { get; set; }
        public DateTimeOffset LastModifiedDateTimeUtc { get; set; }

        public string? OrganisationId { get; set; }
        public string? InviteeEmail { get; set; }
        public string? InvitedByUserId { get; set; }
        public string? InvitationToken { get; set; }
        public int InvitationStatus { get; set; }
        public DateTimeOffset? InvitationStatusUpdateDateTimeUtc { get; set; }

        public OrganisationInvite(
            string organisationId,
            string inviteeEmail,
            string invitedByUserId,
            string invitationToken,
            DateTimeOffset creationDateTimeUtc,
            int invitationStatus,
            DateTimeOffset? lastModifiedDateTimeUtc = default,
            DateTimeOffset? invitationStatusUpdateDateTimeUtc = default)
        {
            if (string.IsNullOrWhiteSpace(organisationId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationId));
            }

            if (string.IsNullOrWhiteSpace(inviteeEmail))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(inviteeEmail));
            }

            if (string.IsNullOrWhiteSpace(invitedByUserId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(invitedByUserId));
            }

            if (string.IsNullOrWhiteSpace(invitationToken))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(invitationToken));
            }

            CreationDateTimeUtc = creationDateTimeUtc;
            LastModifiedDateTimeUtc = lastModifiedDateTimeUtc ?? creationDateTimeUtc;

            PartitionKey = organisationId;
            RowKey = inviteeEmail;

            OrganisationId = organisationId;
            InviteeEmail = inviteeEmail;
            InvitedByUserId = invitedByUserId;
            InvitationToken = invitationToken;
            InvitationStatus = invitationStatus;
            InvitationStatusUpdateDateTimeUtc = invitationStatusUpdateDateTimeUtc;
        }

        //For Serialization / Deserialization purposes
        // ReSharper disable once UnusedMember.Global
        public OrganisationInvite()
        {

        }
    }
}
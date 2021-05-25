using System;
using MessagePack;
using Microsoft.Azure.Cosmos.Table;

namespace Ruzzie.Identity.Storage.Azure.Entities
{
    public class Organisation : TableEntity
    {
        public string? CompanyName { get; set; }

        [IgnoreProperty]
        public string? OrganisationName
        {
            get => CompanyName;
            set => CompanyName = value;
        }

        public string? CreatedByUserId { get; set; }
        public DateTimeOffset CreationDateTimeUtc { get; set; }
        public DateTimeOffset LastModifiedDateTimeUtc { get; set; }

        public Organisation(string organisationName,
            string createdByUserId,
            DateTimeOffset creationDateTimeUtc,
            DateTimeOffset? lastModifiedDateTimeUtc = default)
        {

            if (string.IsNullOrWhiteSpace(organisationName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(organisationName));
            }

            if (string.IsNullOrWhiteSpace(createdByUserId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(createdByUserId));
            }

            OrganisationName = organisationName;
            CompanyName = organisationName;
            CreatedByUserId = createdByUserId;
            CreationDateTimeUtc = creationDateTimeUtc;
            LastModifiedDateTimeUtc = lastModifiedDateTimeUtc ?? creationDateTimeUtc;

            RowKey = organisationName.CreateAlphaNumericKey(KeyGenerators.AlphaNumericKeyGenOptions.TrimInput |
                                                            KeyGenerators.AlphaNumericKeyGenOptions
                                                                .PreserveSpacesAsDashes);
            PartitionKey = RowKey.CalculatePartitionKeyForAlphaNumericRowKey();
        }

        //For Serialization / Deserialization purposes
        // ReSharper disable once UnusedMember.Global
        [SerializationConstructor]
        public Organisation()
        {

        }
    }
}
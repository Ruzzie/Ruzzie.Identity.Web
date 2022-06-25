using System;
using MessagePack;
using Microsoft.Azure.Cosmos.Table;
using Ruzzie.Azure.Storage;

namespace Ruzzie.Identity.Storage.Azure.Entities;

public class Organisation : TableEntity
{
    public static readonly KeyGenerators.AlphaNumericKeyGenOptions AlphaNumericKeyGenOptions = KeyGenerators.AlphaNumericKeyGenOptions.TrimInput |
                                                                                               KeyGenerators.AlphaNumericKeyGenOptions
                                                                                                            .PreserveSpacesAsDashes;

    public string? CompanyName { get; set; }

    [IgnoreProperty]
    public string? OrganisationName
    {
        get => CompanyName;
        set => CompanyName = value;
    }

    public string?        CreatedByUserId         { get; set; }
    public DateTimeOffset CreationDateTimeUtc     { get; set; }
    public DateTimeOffset LastModifiedDateTimeUtc { get; set; }

    public Organisation(string          organisationName,
                        string          createdByUserId,
                        DateTimeOffset  creationDateTimeUtc,
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

        OrganisationName        = organisationName;
        CompanyName             = organisationName;
        CreatedByUserId         = createdByUserId;
        CreationDateTimeUtc     = creationDateTimeUtc;
        LastModifiedDateTimeUtc = lastModifiedDateTimeUtc ?? creationDateTimeUtc;

        var alphaNumericKey = organisationName.CreateAlphaNumericKey(AlphaNumericKeyGenOptions);
        RowKey       = alphaNumericKey;
        PartitionKey = alphaNumericKey.CalculatePartitionKeyForAlphaNumericRowKey();
    }

    //For Serialization / Deserialization purposes
    // ReSharper disable once UnusedMember.Global
    [SerializationConstructor]
    public Organisation()
    {

    }
}
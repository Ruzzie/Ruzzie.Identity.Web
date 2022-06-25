using System;
using Microsoft.Azure.Cosmos.Table;

namespace Ruzzie.Identity.Storage.Azure.Entities;

public class UserOrganisation : TableEntity, IUserOrgProps
{
    public string?        Role                      { get; set; }
    public DateTimeOffset JoinedCreationDateTimeUtc { get; set; }

    public UserOrganisation(string partitionKey, string rowKey, string role, DateTimeOffset joinedCreationDateTimeUtc) : base(partitionKey, rowKey)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(role));
        }

        Role                      = role;
        JoinedCreationDateTimeUtc = joinedCreationDateTimeUtc;
    }

    //For Serialization / Deserialization purposes
    // ReSharper disable once UnusedMember.Global
    public UserOrganisation()
    {

    }
}
using System;
using Microsoft.Azure.Cosmos.Table;
using Ruzzie.Azure.Storage;

// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ruzzie.Identity.Storage.Azure.Entities;

public class UserRegistration : TableEntity
{
    public string? Email     { get; set; }
    public string? Password  { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname  { get; set; }

    public string         SystemFeatureToggles    { get; set; } = string.Empty;
    public DateTimeOffset CreationDateTimeUtc     { get; set; }
    public DateTimeOffset LastModifiedDateTimeUtc { get; set; }
    public int            AccountValidationStatus { get; set; }
    public string?        EmailValidationToken    { get; set; }

    public DateTimeOffset? ValidationStatusUpdateDateTimeUtc { get; set; }

    public string?         PasswordResetToken                  { get; set; }
    public DateTimeOffset? PasswordResetTokenUpdateDateTimeUtc { get; set; }

    //For Serialization / Deserialization purposes
    // ReSharper disable once UnusedMember.Global
    public UserRegistration()
    {

    }

    public UserRegistration(
        string          email,
        string          password,
        string          firstname,
        string          lastname,
        string          emailValidationToken,
        DateTimeOffset  creationDateTimeUtc                 = default,
        DateTimeOffset? lastModifiedDateTimeUtc             = default,
        int             accountValidationStatus             = default,
        DateTimeOffset? validationStatusUpdateDateTimeUtc   = default,
        string?         passwordResetToken                  = default,
        DateTimeOffset? passwordResetTokenUpdateDateTimeUtc = default
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(firstname))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(firstname));
        }

        if (string.IsNullOrWhiteSpace(lastname))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(lastname));
        }

        CreationDateTimeUtc                 = creationDateTimeUtc;
        LastModifiedDateTimeUtc             = lastModifiedDateTimeUtc ?? creationDateTimeUtc;
        AccountValidationStatus             = accountValidationStatus;
        ValidationStatusUpdateDateTimeUtc   = validationStatusUpdateDateTimeUtc;
        Firstname                           = firstname;
        Lastname                            = lastname;
        EmailValidationToken                = emailValidationToken;
        Password                            = password;
        PasswordResetToken                  = passwordResetToken;
        PasswordResetTokenUpdateDateTimeUtc = passwordResetTokenUpdateDateTimeUtc;

        RowKey       = email;
        PartitionKey = email.CreateAlphaNumericPartitionKey().ToString();
        Email        = email;
    }
}
using System;
using FluentAssertions;
using FsCheck;
using NUnit.Framework;
using Ruzzie.Identity.Storage.Azure.Entities;

namespace Ruzzie.Identity.Storage.UnitTests.Azure.Entities;

[TestFixture]
public class UserRegistrationTests
{
    [FsCheck.NUnit.Property]
    public void CtorPropertyTest(
        NonEmptyString  email,
        NonEmptyString  password,
        NonEmptyString  firstname,
        NonEmptyString  lastname,
        NonEmptyString  emailValidationToken,
        DateTimeOffset  creationDateTimeUtc               = default,
        DateTimeOffset? lastModifiedDateTimeUtc           = default,
        int             validationStatus                  = default,
        DateTimeOffset? validationStatusUpdateDateTimeUtc = default)
    {
        try
        {
            var result = new UserRegistration(email.Get, password.Get, firstname.Get, lastname.Get, emailValidationToken.Get, creationDateTimeUtc, lastModifiedDateTimeUtc, validationStatus,
                                              validationStatusUpdateDateTimeUtc);

            result.Should().NotBeNull();
            result.Email.Should().Be(email.Get);
            result.RowKey.Should().Be(email.Get);
            result.PartitionKey.Should().NotBeNullOrEmpty();
        }
        catch (ArgumentException e)
            when( e.Message.StartsWith("Stripped value has a length of 0. Provide a input string with at least 1 ASCII character"))
        {
            //This is a valid case
        }
        catch (ArgumentException e)
            when( e.Message.StartsWith("Value cannot be null or whitespace."))

        {
            //This is a valid case: only control characters or lots of whitespaces
        }
    }
}
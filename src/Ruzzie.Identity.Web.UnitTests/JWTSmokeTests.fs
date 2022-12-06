module Ruzzie.Identity.Web.UnitTests.SmokeTests

open System
open NUnit.Framework
open FsCheck.NUnit
open FluentAssertions
open Microsoft.IdentityModel.Logging
open Ruzzie.Identity.Web.Authentication
open FsCheck
open JWT
open Ruzzie.Identity.Web.DomainTypes.AccountValidation

[<SetUp>]
let Setup() =
    ()

[<Test>]
let SmokeTest() =
    IdentityModelEventSource.ShowPII <- true
    let config =
        { Secret = "psssssssssssssssssssssssssssssssssssssssssssssht"
          ExpirationInMinutes = 10
          Issuer = "testi"
          Audience = "testa" }

    let jwt =
        JWT.generateJWTForUser "101" ["DefaultRole" ; "Reader"] "PVH" ["PVH"] ["PVH"] DateTimeOffset.UtcNow config
    jwt.Should().NotBeEmpty(null) |> ignore

[<Test>]
let createEmailValidationTokenTest() =

    let tokenIssueDate = DateTimeOffset.UtcNow
    let forEmail = "test@test.org"
    let tokenWithDummyEncryption = generateAccountTokenString forEmail tokenIssueDate (fun x -> x) TokenType.ValidateEmail
    tokenWithDummyEncryption.Should().Contain(";;", null) |> ignore

[<Property>]
let ``createEmailValidationtoken does not throw exceptions `` (forEmail: NonEmptyString) tokenIssueDate =
    not (String.IsNullOrWhiteSpace(generateAccountTokenString forEmail.Get tokenIssueDate (fun x -> x) TokenType.ValidateEmail))


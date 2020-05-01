module Ruzzie.Identity.Web.UnitTests.OrganisationTokenTests

open System
open NUnit.Framework
open Ruzzie.Identity.Web.DomainTypes.Organisations
open Ruzzie.Identity.Web.DomainTypes
open FsCheck
open FsCheck.NUnit
open FluentAssertions

[<Property>]
let ``generate invitation token propery quickcheck`` (email: NonEmptyString) (orgId: NonEmptyString) utcNow =
    let tokenStr =
        Organisations.generateOrganisationTokenString email.Get orgId.Get utcNow (fun e -> e)
            OrganisationTokenType.Invitation
    tokenStr.Should().NotBeEmpty(null) |> ignore

[<Test>]
let ``create invitation Token Data from encrypted string Valid Token test ``() =
    let tokenStr =
        Organisations.generateOrganisationTokenString "test@valid.org" "ACME-01"
            (DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1.0))) (fun e -> e) OrganisationTokenType.Invitation

    let createResult =
        Organisations.createToken tokenStr DateTimeOffset.UtcNow (fun d -> d)

    match createResult with
    | Ok token ->
        match token with
        | ValidToken tokenInfo ->
            Assert.AreEqual("test@valid.org", EmailAddressValue.value tokenInfo.ForEmail) |> ignore
            Assert.AreEqual("ACME-01", StringRequiredValue.value tokenInfo.OrganisationId) |> ignore
    | Error err -> Assert.Fail(err.ToString())


[<TestCase(OrganisationTokenType.Invitation)>]
let ``create organisation Token Data from encrypted string Token Type Test `` (tokenType) =
    let tokenStr =
        Organisations.generateOrganisationTokenString "test@valid.org" "ACME-01"
            (DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1.0))) (fun e -> e) tokenType

    let validationTokenResult =
        Organisations.createToken tokenStr DateTimeOffset.UtcNow (fun d -> d)

    match validationTokenResult with
    | Ok token ->
        match token with
        | ValidToken tokenInfo ->
            Assert.AreEqual(tokenType, tokenInfo.TokenType) |> ignore
    | Error err -> Assert.Fail(err.ToString())

[<Property>]
let ``create organisation token should not throw exception`` (tokenStr: NonEmptyString) (utcNow: DateTimeOffset) =
    let validationTokenResult =
        Organisations.createToken tokenStr.Get utcNow (fun d -> d)
    match validationTokenResult with
    | _ -> Assert.IsTrue(true)

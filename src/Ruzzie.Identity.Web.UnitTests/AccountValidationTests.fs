module Ruzzie.Identity.Web.UnitTests.UnitTests.AccountValidationTests

open System
open NUnit.Framework
open Ruzzie.Identity.Web.DomainTypes.AccountValidation
open Ruzzie.Identity.Web.DomainTypes
open FsCheck
open FsCheck.NUnit
open FluentAssertions

[<Property>]
let ``generate validation token propery quickcheck`` (email:NonEmptyString) expires =
    let tokenStr = generateAccountTokenString email.Get expires (fun e -> e) TokenType.ValidateEmail
    tokenStr.Should().NotBeEmpty(null)
    |>ignore

[<Test>]
let ``create validation Token Data from encrypted string Valid Token test ``() =
    let tokenStr =
        AccountValidation.generateAccountTokenString "test@valid.org"
            (DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1.0))) (fun e -> e) TokenType.ValidateEmail

    let validationTokenResult =
        AccountValidation.createToken tokenStr DateTimeOffset.UtcNow (fun d -> d)

    match validationTokenResult with
    | Ok token ->
        match token with
        | ValidToken tokenInfo ->
            Assert.AreEqual("test@valid.org", EmailAddressValue.value tokenInfo.ForEmail) |> ignore
        | ExpiredToken tokenInfo -> Assert.Fail("Expired: " + EmailAddressValue.value tokenInfo.ForEmail) |> ignore
    | Error err -> Assert.Fail(err.ToString())

[<TestCase(TokenType.ValidateEmail)>]
[<TestCase(TokenType.ResetPassword)>]
let ``create validation Token Data from encrypted string Token Type Test `` tokenType =
    let tokenStr =
        AccountValidation.generateAccountTokenString "test@valid.org"
            (DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1.0))) (fun e -> e) tokenType

    let validationTokenResult =
        AccountValidation.createToken tokenStr DateTimeOffset.UtcNow (fun d -> d)

    match validationTokenResult with
    | Ok token ->
        match token with
        | ValidToken tokenInfo ->
            Assert.AreEqual(tokenType, tokenInfo.TokenType) |> ignore
        | ExpiredToken tokenInfo -> Assert.Fail("Expired: " + EmailAddressValue.value tokenInfo.ForEmail) |> ignore
    | Error err -> Assert.Fail(err.ToString())

[<Test>]
let ``create authentication Token Data from encrypted string Expired Token test ``() =
    let tokenStr =
        AccountValidation.generateAccountTokenString "test@valid.org"
            DateTimeOffset.UtcNow (fun e -> e) TokenType.ValidateEmail

    let validationTokenResult =
        AccountValidation.createToken tokenStr (DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(100.0))) (fun d -> d)

    match validationTokenResult with
    | Ok token ->
        match token with
        | ValidToken tokenInfo ->
            Assert.Fail("test@valid.org" + EmailAddressValue.value tokenInfo.ForEmail) |> ignore
        | ExpiredToken tokenInfo -> Assert.AreEqual("test@valid.org", EmailAddressValue.value tokenInfo.ForEmail) |> ignore
    | Error err -> Assert.Fail(err.ToString())

[<Property>]
let ``create validation token should not throw exception`` (tokenStr:NonEmptyString) utcNow =
    let validationTokenResult =
        AccountValidation.createToken tokenStr.Get utcNow (fun d -> d)
    match validationTokenResult with
    _ -> Assert.IsTrue(true)
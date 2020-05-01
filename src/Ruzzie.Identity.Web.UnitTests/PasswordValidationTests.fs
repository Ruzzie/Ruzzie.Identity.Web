module Ruzzie.Identity.Web.UnitTests.PasswordValidationTests

open Ruzzie.Identity.Web
open FluentAssertions
open NUnit.Framework

[<TestCase("kasjhfkjdfgfdkh123123", "henk@test.com", "Henk", "Test", true)>]
[<TestCase("Ik houd van taart!", "Ad@test.com", "Ad", "Test", true)>]
[<TestCase("Hoofdletter van test tennis!", "Ad@test.com", "Ad", "T", true)>]
[<TestCase("Toofdletter van test tennis!", "Ad@test.com", "Ad", "T", true)>]
[<TestCase("adIsDeBeste!", "Ad@test.com", "Ad", "Test", false)>]
[<TestCase("ik houd van test badminton!", "Ad@test.com", "Ad", "Test", false)>]
[<TestCase("ik houd van test tennis!", "Ad@test.com", "Ad", "Test", false)>]
let ``create passwords `` password email firstname lastname (isOk:bool) =
    let createRes = PasswordValue.create password None email firstname lastname
    match createRes with
    |Ok _ -> isOk.Should().BeTrue(null) |> ignore
    |Error e -> isOk.Should().BeFalse(e.ToString()) |> ignore

//todo create test for Paul12345@ and validate the correct error message
module Ruzzie.Identity.Web.UnitTests.UseCases.Common
open Ruzzie.Identity.Web
open FsCheck
open NUnit.Framework
open FluentAssertions

[<Test>]
let ``create url must url encode query``() =
    let url = UseCases.Shared.createUrlWithOneQuery "https" "localhost" "test" ("value","  //")
    Assert.AreEqual("https://localhost/test?value=%20%20%2F%2F", url)

[<FsCheck.NUnit.Property>]
let ``create url must url encode query property test``(queryStringValue: NonEmptyString) =
    let url = UseCases.Shared.createUrlWithOneQuery "https" "localhost" "test" ("value", queryStringValue.Get)
    url.Should().StartWith("https://localhost/test?value=", null) |> ignore
module Ruzzie.Identity.Web.UnitTests.EmailTests

open System.Net
open NUnit.Framework
open Ruzzie.Identity.Web.Email

[<Test>]
[<Category("Email.IntegrationTests")>]
[<Ignore("Use when needed")>]
let `` Send email IntegrationTest``() =

    let domain = "XXXX"
    let apiKey = "XXXX"

    let emailServiceMailgun = { Send = Mailgun.mailgunSendMail apiKey domain }

    let testEmail =
        createSendEmailRequest "Link Bi <noreply@ms.pvhlink.com>" "dorus.verhoeckx+inttest@gmail.com"
            "Integration Test" "Test text content" "Test <b>HTML</b> content"


    match testEmail with
    | Ok sendRequest ->
        let sendResult = sendEmail emailServiceMailgun sendRequest

        match sendResult with
        | Ok status -> Assert.AreEqual(status, HttpStatusCode.OK)
        | Error(status, errMsg) -> Assert.Fail(status.ToString() + " " + errMsg)

    | Error errors -> Assert.Fail(errors.ToString())

namespace Ruzzie.Identity.Web

open System.Net
open ResultLib
open DomainTypes
open RestSharp
open RestSharp.Authenticators

module Email =

    type SendEmailRequest =
        { From: StringNonEmpty.T
          To: EmailAddressValue.T
          Subject: StringNonEmpty.T
          TextContent: StringNonEmpty.T
          HtmlContent: StringNonEmpty.T }


    type EmailService = {
        Send : SendEmailRequest -> Result<HttpStatusCode, HttpStatusCode * string>
    }

    let createSendEmailRequest from ``to`` subject text html =
        let create from toEmail subject text html =
            {From = from
             To = toEmail
             Subject = subject
             TextContent = text
             HtmlContent = html}

        create
        <!> (StringNonEmpty.create from (Some({FieldName = Some("createSendEmailRequest.from")
                                               Details = None})))
        <.*.> (EmailAddressValue.create ``to`` (Some({FieldName = Some("createSendEmailRequest.to")
                                                      Details = None})))
        <*.> (StringNonEmpty.create subject (Some({FieldName = Some("createSendEmailRequest.subject")
                                                   Details = None})))
        <*.> (StringNonEmpty.create text (Some({FieldName = Some("createSendEmailRequest.text")
                                                Details = None})))
        <*.> (StringNonEmpty.create html (Some({FieldName = Some("createSendEmailRequest.html")
                                                Details = None})))
    let toErrorKindResult sendResult =
         Result.mapError (fun (status: HttpStatusCode, message) ->
                                            [Unexpected(status |> int, message)]) sendResult

    let sendEmail emailService emailRequest =
        emailService.Send emailRequest

    module Mailgun =

        [<Literal>]
        let baseUrl = "https://api.eu.mailgun.net/v3"

        let private client =  RestSharp.RestClient(baseUrl) :> IRestClient

        let mailgunSendMail apiKey mailDomain emailRequest =

            let sendEmail emailRequest =
                client.Authenticator <- HttpBasicAuthenticator("api", apiKey)

                let request = RestRequest("{domain}/messages", Method.POST)
                request.AddParameter(Parameter("domain", mailDomain, ParameterType.UrlSegment)) |> ignore
                request.AddParameter("from", (StringNonEmpty.value emailRequest.From)) |> ignore
                request.AddParameter("to", (EmailAddressValue.value emailRequest.To)) |> ignore
                request.AddParameter("subject", (StringNonEmpty.value emailRequest.Subject)) |> ignore
                request.AddParameter("text", (StringNonEmpty.value emailRequest.TextContent)) |> ignore
                request.AddParameter("html", (StringNonEmpty.value emailRequest.HtmlContent)) |> ignore

                let response = client.Execute(request)

                if response.IsSuccessful then
                    Ok(response.StatusCode)
                else
                    Error(response.StatusCode, response.ErrorMessage + ";Content: [" + response.Content + "]")


            (sendEmail emailRequest)

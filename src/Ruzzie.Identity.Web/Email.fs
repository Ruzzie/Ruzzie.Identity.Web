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
          ReplyTo: EmailAddressValue.T option
          Subject: StringNonEmpty.T
          TextContent: StringNonEmpty.T
          HtmlContent: string}


    type EmailService = {
        Send : SendEmailRequest -> Result<HttpStatusCode, HttpStatusCode * string>
    }

    let createSendEmailRequest from ``to`` (replyTo:string option) subject text html=
        let create from toEmail replyTo subject text html =
            {From = from
             To = toEmail
             ReplyTo = replyTo
             Subject = subject
             TextContent = text
             HtmlContent = html}

        create
        <!> (StringNonEmpty.create from (Some({FieldName = "createSendEmailRequest.from"
                                               Details = None})))
        <.*.> (EmailAddressValue.create ``to`` (Some({FieldName = "createSendEmailRequest.to"
                                                      Details = None})))
        <*.> match replyTo with
             |Some replyToStr -> (EmailAddressValue.create replyToStr (Some({FieldName = "createSendEmailRequest.replyTo"
                                                                             Details = None}))) |> Result.map (fun k -> Some k)
             |None -> Ok None
        <*.> (StringNonEmpty.create subject (Some({FieldName = "createSendEmailRequest.subject"
                                                   Details = None})))
        <*.> (StringNonEmpty.create text (Some({FieldName = "createSendEmailRequest.text"
                                                Details = None})))
        <*.> (Ok html)
    let toErrorKindResult sendResult =
         Result.mapError (fun (status: HttpStatusCode, message) ->
                                            [Unexpected(status |> int, message)]) sendResult

    let sendEmail emailService emailRequest =
        emailService.Send emailRequest

    module Mailgun =

        [<Literal>]
        let baseUrl = "https://api.eu.mailgun.net/v3"
                                                
        let createMailGunSendEmailRestRequest (emailRequest : SendEmailRequest) (mailDomain : string)  =
            let request = RestRequest("{domain}/messages", Method.POST)
            request.AddParameter(Parameter("domain", mailDomain, ParameterType.UrlSegment)) |> ignore
            request.AddParameter("from", (StringNonEmpty.value emailRequest.From)) |> ignore
            request.AddParameter("to", (EmailAddressValue.value emailRequest.To)) |> ignore
            request.AddParameter("subject", (StringNonEmpty.value emailRequest.Subject)) |> ignore
            request.AddParameter("text", (StringNonEmpty.value emailRequest.TextContent)) |> ignore

            if not (System.String.IsNullOrWhiteSpace(emailRequest.HtmlContent)) then
                request.AddParameter("html", emailRequest.HtmlContent) |> ignore
            else
                ()

            match emailRequest.ReplyTo with
            |Some replyTo -> request.AddParameter("h:Reply-To", (EmailAddressValue.value replyTo)) |> ignore
            |None -> ()
            
            request
                                                                  
        let mailgunSendMail apiKey mailDomain (emailRequest:SendEmailRequest) =
            
            let restClient = RestClient(baseUrl) :> IRestClient
            restClient.Authenticator <- HttpBasicAuthenticator("api", apiKey)
           
            let request = createMailGunSendEmailRestRequest emailRequest mailDomain

            let response = restClient.Execute(request)

            if response.IsSuccessful then
                Ok(response.StatusCode)
            else
                Error(response.StatusCode, response.ErrorMessage + ";Content: [" + response.Content + "]")

           

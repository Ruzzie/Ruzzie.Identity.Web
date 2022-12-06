namespace Ruzzie.Identity.Web.UseCases

open System
open System.Text.Encodings.Web
open Ruzzie.Identity.Web
open Ruzzie.Identity.Web.Email
open Microsoft.AspNetCore.Http

module Shared =

    [<CLIMutable>] //TODO: Move to a settings module or something
    type ClientAppConfig =
        { Host: string
          ConfirmRegistrationPath: string
          ResetPasswordPath: string
          AcceptOrganisationInvitePath: string
          WithEmailActivation: bool}

    type EmailTemplates =
        {
            ///toEmail, activateActionUrl
            CreateUserRegistrationActivationMail: string -> string -> Result<SendEmailRequest, ErrorKind list>
            ///toEmail, resetActionUrl
            CreateUserPasswordForgetMail: string -> string -> Result<SendEmailRequest, ErrorKind list>
            ///byUser, forOrganisation, toEmail
            CreateInviteUserToOrganisationEmail: ApiTypes.User -> ApiTypes.UserOrganisation -> string -> string -> Result<SendEmailRequest, ErrorKind list>
        }

    let createJWT email currentOrgKey orgKeysWhereOwner orgKeysWhereMember utcNow jwtConfig =
        if String.IsNullOrWhiteSpace(email) then
            Error
                (ErrorKind.CannotBeEmpty
                    (Some
                       { ErrInfo.FieldName = "email"
                         ErrInfo.Details = None }))
        else
            try
                let genJwt =
                    Authentication.JWT.generateJWTForUser email ("User" :: []) currentOrgKey orgKeysWhereOwner orgKeysWhereMember utcNow jwtConfig
                Ok(genJwt)
            with ex -> createInvalidErrorWithExn "email" "createDefaultJWT.unexpectedError" ex

    let private createUrl scheme host path query =
        let activationLinkBuilder = UriBuilder(scheme, host)
        activationLinkBuilder.Path <- path
        activationLinkBuilder.Query <- query
        activationLinkBuilder.Uri.ToString()

    let createUrlWithOneQuery scheme host path (queryKey, queryValue) =
        createUrl scheme host path queryKey + "=" + UrlEncoder.Default.Encode(queryValue)

    let registerUserActivationUrlFunc (httpContext: HttpContext) clientAppConfig token =
        createUrlWithOneQuery httpContext.Request.Scheme clientAppConfig.Host clientAppConfig.ConfirmRegistrationPath
            ("validationToken", token)

    let getAuthorizedUserId (httpContext: HttpContext) =
        let user = httpContext.User
        if user = null then
            Error Unauthorized
        else if user.Identity = null then
            Error Unauthorized
        else if String.IsNullOrWhiteSpace(user.Identity.Name) then
            Error Unauthorized
        else
            let userId = user.Identity.Name
            (EmailAddressValue.create userId
                 (Some
                     { DomainTypes.ErrInfo.FieldName = "email"
                       DomainTypes.ErrInfo.Details = None }))

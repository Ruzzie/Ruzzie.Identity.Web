namespace Ruzzie.Identity.Web
open ResultLib
open Ruzzie.Identity.Web.ApiTypes
open Microsoft.AspNetCore.Mvc.ModelBinding

module Validation =

    let addErrorToModelState (modelState: ModelStateDictionary) (err) =
        match err with
        | TooShort errInfoOption ->
            modelState.AddModelError(fieldNameOrGeneric errInfoOption, toErrorMessage err errInfoOption)
        | TooLong errInfoOption ->
            modelState.AddModelError(fieldNameOrGeneric errInfoOption, toErrorMessage err errInfoOption)
        | Invalid errInfoOption ->
            modelState.AddModelError(fieldNameOrGeneric errInfoOption, toErrorMessage err errInfoOption)
        | CannotBeEmpty errInfoOption ->
            modelState.AddModelError(fieldNameOrGeneric errInfoOption, toErrorMessage err errInfoOption)
        | InvalidToken errInfoOption ->
            modelState.AddModelError(fieldNameOrGeneric errInfoOption, toErrorMessage err errInfoOption)
        | Unexpected (status, message) ->
            modelState.AddModelError("Unexpected", status.ToString() + ":" +  message)
        | Unauthorized ->
            modelState.AddModelError("Unauthorized", System.String.Empty)

    let addErrorListToModelState (modelState: ModelStateDictionary) (errors) =
        for err in errors do
            addErrorToModelState modelState err

    module RegisterUserInput =
        let private createRegisterUserRequest email firstname lastname password =
            { DomainTypes.RegisterUserRequest.Email = email
              DomainTypes.RegisterUserRequest.Firstname = firstname
              DomainTypes.RegisterUserRequest.Lastname = lastname
              DomainTypes.RegisterUserRequest.PasswordValue = password }

        let validateRegisterUserInput (input: ApiTypes.RegisterUserReq) =
            let emailResult =
                DomainTypes.EmailAddressValue.create input.email
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "email"
                          DomainTypes.ErrInfo.Details = None })

            let firstNameResult =
                DomainTypes.StringRequiredValue.create input.firstname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "firstname"
                          DomainTypes.ErrInfo.Details = None })

            let lastNameResult =
                DomainTypes.StringRequiredValue.create input.lastname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "lastname"
                          DomainTypes.ErrInfo.Details = None })

            let passwordResult =
                DomainTypes.PasswordValue.create input.password
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "password"
                          DomainTypes.ErrInfo.Details = None }) input.email input.firstname input.lastname

            createRegisterUserRequest <!> emailResult <.*.> firstNameResult
                                      <*.> lastNameResult <*.> passwordResult

        let validateAcceptInviteUserInput (input: ApiTypes.AcceptOrganisationInviteReq) email =

            let emailResult = Ok(email)

            let firstNameResult =
                DomainTypes.StringRequiredValue.create input.firstname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "firstname"
                          DomainTypes.ErrInfo.Details = None })

            let lastNameResult =
                DomainTypes.StringRequiredValue.create input.lastname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "lastname"
                          DomainTypes.ErrInfo.Details = None })

            let passwordResult =
                DomainTypes.PasswordValue.create input.password
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "password"
                          DomainTypes.ErrInfo.Details = None }) (EmailAddressValue.value email) input.firstname input.lastname

            createRegisterUserRequest <!> emailResult <.*.> firstNameResult
                                      <*.> lastNameResult <*.> passwordResult

    module AuthenticateUserInput =
        let private createAuthenticateUserRequest email password =
            { DomainTypes.AuthenticateUserRequest.Email = email
              DomainTypes.AuthenticateUserRequest.PasswordValue = password }

        let validateInput (input: ApiTypes.AuthenticateUserReq) =
            let emailResult =
                DomainTypes.EmailAddressValue.create input.email
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "email"
                          DomainTypes.ErrInfo.Details = None })

            let passwordResult =
                DomainTypes.PasswordValue.create input.password
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "password"
                          DomainTypes.ErrInfo.Details = None }) input.email input.email input.email

            createAuthenticateUserRequest <!> emailResult <.*.> passwordResult

    module AddOrganisationUserInput =

        let private createAddOrganisationRequest userId orgName =
            {DomainTypes.AddOrganisationRequest.UserId = userId
             DomainTypes.AddOrganisationRequest.OrganisationName = orgName}

        let validateInput (inputReq: ApiTypes.AddOrganisationReq) userId =
             let organisationNameResult =
                DomainTypes.StringRequiredValue.create inputReq.organisationName
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "organisationName"
                          DomainTypes.ErrInfo.Details = None })

             organisationNameResult .=> (fun orgName -> Ok(createAddOrganisationRequest userId orgName))

    module UpdateUserInput =
        let private createUpdateUserRequest userId firstname lastname =
            { DomainTypes.UpdateUserRequest.UserId = userId
              DomainTypes.UpdateUserRequest.Firstname = firstname
              DomainTypes.UpdateUserRequest.Lastname = lastname }

        let validateInput (input: ApiTypes.UpdateUserReq) (userId:EmailAddressValue.T) =

            let userIdResult = Ok(userId)

            let firstNameResult =
                DomainTypes.StringRequiredValue.create input.firstname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "firstname"
                          DomainTypes.ErrInfo.Details = None })

            let lastNameResult =
                DomainTypes.StringRequiredValue.create input.lastname
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "lastname"
                          DomainTypes.ErrInfo.Details = None })

            createUpdateUserRequest <!> userIdResult <.*.> firstNameResult
                                      <*.> lastNameResult

    module InviteUserToOrganisationInput =
        let private createInviteUserToOrganisationRequest userId inviteeEmail organisationId =
            { ByUserId = userId
              InviteeEmail = inviteeEmail
              OrganisationId = organisationId }

        let validateInput inviteeEmailStr orgIdStr (userId:EmailAddressValue.T) =

            let userIdResult = Ok(userId)

            let inviteeEmailResult =
                DomainTypes.EmailAddressValue.create inviteeEmailStr
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "inviteeEmail"
                          DomainTypes.ErrInfo.Details = None })

            let organisationIdResult =
                DomainTypes.StringRequiredValue.create orgIdStr
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "organisationId"
                          DomainTypes.ErrInfo.Details = None })

            createInviteUserToOrganisationRequest <!> userIdResult <.*.> inviteeEmailResult
                                      <*.> organisationIdResult

    module GetAllUsersForOrganisationInput =

         let private createGetAllUsersForOrganisationInputRequest requestedByUserId organisationId =
            { RequestedByUserId = requestedByUserId
              OrganisationId = organisationId }

         let validateInput orgIdStr (requestedByUserId:EmailAddressValue.T) =

            let requestedByUserIdResult = Ok(requestedByUserId)

            let organisationIdResult =
                DomainTypes.StringRequiredValue.create orgIdStr
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "organisationId"
                          DomainTypes.ErrInfo.Details = None })

            createGetAllUsersForOrganisationInputRequest <!> requestedByUserIdResult
                                      <*.> organisationIdResult

    module DeleteUserFromOrganisationInput =

         let private createRequest requestedByUserId userId organisationId =
             { DomainTypes.DeleteUserFromOrganisationRequest.RequestedByUserId = requestedByUserId
               UserId = userId
               OrganisationId = organisationId}

         let validateInput orgIdStr userIdStr (requestedByUserId:EmailAddressValue.T) =

             let requestedByUserIdResult = Ok(requestedByUserId)

             let organisationIdResult =
                DomainTypes.StringRequiredValue.create orgIdStr
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "organisationId"
                          DomainTypes.ErrInfo.Details = None })

             let userIdResult =
                DomainTypes.EmailAddressValue.create userIdStr
                    (Some
                        { DomainTypes.ErrInfo.FieldName = "userId"
                          DomainTypes.ErrInfo.Details = None })

             createRequest <!> requestedByUserIdResult <.*.> userIdResult
                                      <*.> organisationIdResult
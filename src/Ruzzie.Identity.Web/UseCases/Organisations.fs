namespace Ruzzie.Identity.Web.UseCases

open Ruzzie.Identity.Web
open System
open System.Collections.Generic
open Ruzzie.Identity.Web.ApiTypes
open Ruzzie.Identity.Web.DomainTypes.Organisations
open Email
open Users
open ResultLib
open Shared
open Microsoft.Extensions.Logging
open Ruzzie.Identity.Storage.Azure
open Ruzzie.Identity.Storage.Azure.Entities

module Organisations =

    let getOrganisationFromUser organisationId (user: ApiTypes.User) =
        let findResult =
            List.tryFind (fun (org: ApiTypes.UserOrganisation) -> org.id = StringRequiredValue.value organisationId)
                user.organisations
        match findResult with
        | Some org -> Ok(org)
        | None -> Error Unauthorized

    let userIsInOrganisation (orgRepository: IOrganisationRepository) orgId userId =
        try
            //Check if the user is in the given organisation
            Ok(orgRepository.UserIsInOrganisation((StringRequiredValue.value orgId), (EmailAddressValue.value userId)))
        with ex ->
            createInvalidErrorWithExn "email" "userIsInOrganisation.unexpectedError" ex

    let upsertOrganisationInvite (orgRepository: IOrganisationRepository) utcNow entity =
        try
            Ok(orgRepository.UpsertOrganisationInvite(entity, Nullable utcNow))
        with ex ->
            createInvalidErrorWithExn "organisation" "upsertOrganisationInvite.unexpectedError" ex

    let getOrganisationInvite (orgRepository: IOrganisationRepository) orgId userId =
        try
            let entity =
                orgRepository.GetOrganisationInvite(StringRequiredValue.value orgId, EmailAddressValue.value userId)
            if (not (entity = null))
            then Ok(entity)
            else createInvalidError "organisationId" ("getOrganisationInvite.doesNotExist" :: [])
        with ex ->
            createInvalidErrorWithExn "organisationId" "getOrganisationInvite.unexpectedError" ex

    let getAllPendingOrganisationInvites (orgRepository: IOrganisationRepository) organisationId =
        try
            let entity =
                orgRepository.GetAllOrganisationInvites(StringRequiredValue.value organisationId, OrganisationInviteStatus.Pending |> int)
            if (not (entity = null)) then
                Ok(entity)
            else
                Ok(List.Empty :> IReadOnlyList<OrganisationInvite>)
        with ex ->
            createInvalidErrorWithExn "organisationId" "getAllPendingOrganisationInvites.unexpectedError" ex

    let getAllUsersForOrganisationId (orgRepository: IOrganisationRepository) organisationId =
        try
            let usersForOrganisation =
                orgRepository.GetUsersForOrganisation(organisationId)
            if (not (usersForOrganisation = null))
            then Ok(usersForOrganisation)
            else createInvalidError "organisationId" ("getAllUsersForOrganisation.doesNotExist" :: [])
        with ex ->
            createInvalidErrorWithExn "organisationId" "getAllUsersForOrganisation.unexpectedError" ex

    ///Deletes an organisation and removes all users from the organisation
    let deleteOrganisation (orgRepository: IOrganisationRepository) organisationId =
        try
            orgRepository.DeleteOrganisation(organisationId)
            Ok(ignore true)
        with ex ->
            createInvalidErrorWithExn "organisationId" "deleteOrganisation.unexpectedError" ex

    let deleteUserFromOrganisationDb (orgRepository: IOrganisationRepository) orgId userId =
        try
            orgRepository.DeleteUserFromOrganisation(EmailAddressValue.value userId, StringRequiredValue.value orgId)
            Ok(ignore true)
        with ex ->
            createInvalidErrorWithExn "organisationId" "deleteUserFromOrganisationDb.unexpectedError" ex

    let deleteUserInviteFromOrganisationDb (orgRepository: IOrganisationRepository) orgId userId =
        try
            orgRepository.DeleteOrganisationInvite(StringRequiredValue.value orgId, EmailAddressValue.value userId)
            Ok(ignore 1)
        with ex ->
            createInvalidErrorWithExn "organisationId" "deleteUserInviteFromOrganisationDb.unexpectedError" ex

    let createOrganisationInviteToken utcNow encryptFunc (req: InviteUserToOrganisationRequest) =
        let email = (EmailAddressValue.value req.InviteeEmail)
        let orgId = (StringRequiredValue.value req.OrganisationId)
        try
            Ok
                (DomainTypes.Organisations.generateOrganisationTokenString email orgId utcNow encryptFunc
                     OrganisationTokenType.Invitation)
        with ex ->
            createInvalidErrorWithExn "inviteeEmail" "organisationInviteToken.unexpectedError" ex

    let inviteUserToOrganisation
        utcNow
        (userRepository: IUserRepository)
        (orgRepository: IOrganisationRepository)
        encryptFunc
        emailService
        emailTemplates
        inviteAcceptUrlFunc
        (logger: ILogger)
        (req: InviteUserToOrganisationRequest)
        =

        let currentUserResult = getUserInfo utcNow userRepository orgRepository req.ByUserId
        currentUserResult .=>. getOrganisationFromUser req.OrganisationId
        .=> (fun (user, org) ->

            let inviteeIsAlreadyInOrg = userIsInOrganisation orgRepository req.OrganisationId req.InviteeEmail

            match inviteeIsAlreadyInOrg with
            | Ok alreadyInOrg ->
                if alreadyInOrg then
                    createInvalidError "inviteeEmail" ("alreadyInOrganisation" :: [])
                else
                    let createOrganisationInviteEntity tokenStr =
                        OrganisationInvite
                            (org.id, EmailAddressValue.value req.InviteeEmail, user.id, tokenStr, utcNow,
                             OrganisationInviteStatus.Pending |> int)

                    //Store invitationId with status pending, timestamp
                    let runResult =
                        //1. Generate Encrypted Invite Token
                        createOrganisationInviteToken utcNow encryptFunc req
                        .=> (fun tokenStr ->
                            upsertOrganisationInvite orgRepository utcNow (createOrganisationInviteEntity tokenStr))
                        |> toListOfError
                        //3. Create Email if OK
                        .=> (fun inviteEntity ->
                            emailTemplates.CreateInviteUserToOrganisationEmail user org (EmailAddressValue.value req.InviteeEmail)
                                (inviteAcceptUrlFunc inviteEntity.InvitationToken))
                        //4. Send email if OK
                        .=> (fun emailToSend -> sendEmail emailService emailToSend |> toErrorKindResult)

                    match runResult with
                    | Ok statusCode -> Ok(statusCode.ToString())
                    | Error errList ->
                        let errLogString = Log.logErrors logger Log.Events.SendEmailErrorEvent errList
                        Ok(errLogString)
            | Error error -> Error error)


    let acceptOrganisationInvite
        utcNow
        passwordHasher
        (userRepository: IUserRepository)
        (orgRepository: IOrganisationRepository)
        decrypt
        encrypt
        jwtConfig
        withEmailActivation
        emailService
        emailTemplates
        activationUrlFunc
        logger
        req
        =

        let register registerUserRequest =
            UseCases.Users.registerUser utcNow passwordHasher userRepository encrypt jwtConfig withEmailActivation emailService emailTemplates
                activationUrlFunc logger registerUserRequest |> toListOfError

        Organisations.createToken req.invitationToken utcNow decrypt
        |> toListOfError
        .=> (fun orgToken ->
            match orgToken with
            | ValidToken tokenInfo ->
                let email = tokenInfo.ForEmail

                //Check if the invite is in the database
                let compareTokenAndStatus =
                    getOrganisationInvite orgRepository tokenInfo.OrganisationId email
                    .=> (fun orgInviteEntity ->
                        if orgInviteEntity.InvitationStatus = (OrganisationInviteStatus.Accepted |> int) then
                            createErrorWithErrInfo InvalidToken "invitationToken" ("invitationAlreadyAccepted" :: [])
                        else
                            let tokenFromDbRes = Organisations.createToken orgInviteEntity.InvitationToken utcNow decrypt
                            tokenFromDbRes
                            .=> (fun token ->
                                match token with
                                | ValidToken tokenInfoFromDb ->
                                    if tokenInfoFromDb = tokenInfo then
                                        //Ok! same Tokens!
                                        Ok(orgInviteEntity)
                                    else
                                        createErrorWithErrInfo InvalidToken "invitationToken"
                                            ("differentFromStoredToken" :: [])))
                    |> toListOfError

                (Validation.RegisterUserInput.validateAcceptInviteUserInput req email .=> register)
                .<|>. compareTokenAndStatus .=> (fun registerResponse -> Ok(tokenInfo, registerResponse))

        )
        .=> (fun (tokenInfo, (registerResp, orgInviteEntity)) ->

            UseCases.Users.addUserToOrganisationWithRole utcNow userRepository orgRepository tokenInfo.ForEmail
                tokenInfo.OrganisationId "Default"
            .=> (fun _ ->
                orgInviteEntity.InvitationStatus <- (OrganisationInviteStatus.Accepted |> int)
                orgInviteEntity.InvitationStatusUpdateDateTimeUtc <- Nullable utcNow
                orgInviteEntity.InvitationToken <- ""
                upsertOrganisationInvite orgRepository utcNow orgInviteEntity .=> (fun _ -> Ok registerResp))

            |> toListOfError)


    let safeConvertInvitationStatus (statusInt:int) =
        try
             enum<OrganisationInviteStatus>(statusInt)
        with _ ->
            OrganisationInviteStatus.None
    let createOrganisationInvitesApiType invitesEntities =
          [
              for (invite : Entities.OrganisationInvite) in invitesEntities do
                  {
                      ApiTypes.OrganisationInvite.inviteeEmail = invite.InviteeEmail
                      ApiTypes.OrganisationInvite.inviteStatus = safeConvertInvitationStatus invite.InvitationStatus
                      ApiTypes.OrganisationInvite.invitedBy = invite.InvitedByUserId
                      ApiTypes.OrganisationInvite.createdTimestamp = invite.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                      ApiTypes.OrganisationInvite.lastModifiedTimestamp = invite.LastModifiedDateTimeUtc.ToUnixTimeMilliseconds()
                  }
          ]

    let createOrganisationUserApiType userRepository orgUserEntities =

            let mutable resultList = List<ApiTypes.OrganisationUser>()

            for (orgUser :  Entities.OrganisationUser) in orgUserEntities do
                let userRegistrationRes = getUserByEmailStr userRepository orgUser.RowKey

                match userRegistrationRes with
                     | Ok userEntity ->

                          resultList.Add {
                                            ApiTypes.OrganisationUser.id = orgUser.RowKey
                                            ApiTypes.OrganisationUser.email = orgUser.RowKey
                                            ApiTypes.OrganisationUser.userRole = orgUser.Role
                                            ApiTypes.OrganisationUser.userJoinedTimestamp = orgUser.JoinedCreationDateTimeUtc.ToUnixTimeMilliseconds()
                                            firstname = userEntity.Firstname
                                            lastname = userEntity.Lastname
                                            createdTimestamp = userEntity.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                                            lastModifiedTimestamp = userEntity.LastModifiedDateTimeUtc.ToUnixTimeMilliseconds()
                                          }
                     | Error e -> ignore e
            //todo: figure out how to do this 'functionally' -> list.fold
            resultList

    let getAllUsersForOrganisation
        utcNow
        (userRepository: IUserRepository)
        (orgRepository: IOrganisationRepository)
        (req: GetAllUsersForOrganisationRequest)
        =

        //is authorized?
        userIsInOrganisation orgRepository req.OrganisationId req.RequestedByUserId
        .=> (fun isInOrg ->
             if isInOrg then
                 getAllUsersForOrganisationId orgRepository (StringRequiredValue.value req.OrganisationId)
             else Error Unauthorized
             )
        .=> (fun allUsers ->

                //Org details
                let getOrganisationRes = getOrganisationById orgRepository req.OrganisationId

                getOrganisationRes .=> (fun org ->
                                          Ok {
                                                ApiTypes.Organisation.id = org.RowKey
                                                name = org.OrganisationName
                                                createdByUserId = org.CreatedByUserId
                                                createdTimestamp = org.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                                                lastModifiedTimestamp = org.LastModifiedDateTimeUtc.ToUnixTimeMilliseconds()
                                                users = []
                                                pendingInvites = []
                                           }
                                        )
                //Get all pending invites for the organisation
                .<|>. getAllPendingOrganisationInvites orgRepository req.OrganisationId
                .=> (fun (apiOrganisation, pendingInvites) ->
                        Ok
                            {
                                apiOrganisation with pendingInvites = createOrganisationInvitesApiType pendingInvites
                            }
                    )
                //Add All users in the organisation
                .=> (fun (apiOrganisation) ->
                        Ok
                            {
                                apiOrganisation with users = List.ofSeq (createOrganisationUserApiType userRepository allUsers)
                            }
                    )
            )

    let deleteUserFromOrganisation
        (orgRepository: IOrganisationRepository)
        (req: DeleteUserFromOrganisationRequest)
        =
        //is authorized?
        userIsInOrganisation orgRepository req.OrganisationId req.RequestedByUserId
        .=> (fun isInOrg ->
             if isInOrg then
                 deleteUserFromOrganisationDb orgRepository req.OrganisationId req.UserId
             else Error Unauthorized
             )

    let deleteUserInviteFromOrganisation
        (orgRepository: IOrganisationRepository)
        (req: DeleteUserFromOrganisationRequest)
        =
        //is authorized?
        userIsInOrganisation orgRepository req.OrganisationId req.RequestedByUserId
        .=> (fun isInOrg ->
             if isInOrg then
                 deleteUserInviteFromOrganisationDb orgRepository req.OrganisationId req.UserId
             else Error Unauthorized
             )
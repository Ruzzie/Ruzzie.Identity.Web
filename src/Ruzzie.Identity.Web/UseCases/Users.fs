namespace Ruzzie.Identity.Web.UseCases

open Ruzzie.Identity.Web
open Ruzzie.Identity.Web.UseCases.Shared
open Ruzzie.Identity.Web.ApiTypes
open Ruzzie.Identity.Web.DomainTypes.AccountValidation
open System
open Ruzzie.Identity.Web.Email
open Microsoft.Extensions.Logging
open Ruzzie.Identity.Storage.Azure
open Ruzzie.Identity.Storage.Azure.Entities
open Ruzzie.Identity.Web.Security

module Users =

    let userExist email (repository: IUserRepository) =
        try
            //Check if the user already exists
            Ok(repository.UserExists((EmailAddressValue.value email)))
        with ex -> createInvalidErrorWithExn "email" "userExists.unexpectedError" ex

    let userExists2 repository email = userExist email repository .=> (fun exists -> Ok(exists, email))

    let insertNewUser (repository: IUserRepository) userEntity =
        try
            let createdEntity = repository.InsertNewUser userEntity
            if (createdEntity = null) then
                createInvalidError "email" ("insertNewUser.errorGotNullResult" :: [])
            else
                Ok(createdEntity)
        with ex -> createInvalidErrorWithExn "email" "insertNewUser.unexpectedError" ex

    let getUserByEmail (repository: IUserRepository) email =
        try
            let userEntity = repository.GetUserByEmail(DomainTypes.EmailAddressValue.value email)
            if (not (userEntity = null)) then
                Ok(userEntity)
            else
                createInvalidError "email" ("getUserByEmail.doesNotExist" :: [])
        with ex -> createInvalidErrorWithExn "email" "getUserByEmail.unexpectedError" ex

    let getUserByEmailStr (repository: IUserRepository) emailStr =
        try
            let userEntity = repository.GetUserByEmail(emailStr)
            if (not (userEntity = null)) then
                Ok(userEntity)
            else
                createInvalidError "email" ("getUserByEmailStr.doesNotExist" :: [])
        with ex -> createInvalidErrorWithExn "email" "getUserByEmailStr.unexpectedError" ex

    let updateUser (repository: IUserRepository) utcNow userEntity =
        try
            let createdEntity = repository.UpdateUser(userEntity, Nullable utcNow)
            if (createdEntity = null) then
                createInvalidError "email" ("updateUser.errorGotNullResult" :: [])
            else
                Ok(createdEntity)
        with ex -> createInvalidErrorWithExn "email" "updateUser.unexpectedError" ex

    let deleteUser (repository: IUserRepository) userId =
        try
            repository.DeleteUser(EmailAddressValue.value userId)
            Ok(ignore true)
        with ex -> createInvalidErrorWithExn "email" "deleteUser.unexpectedError" ex

    let createPasswordResetToken utcNow encrypt email =
        let emailStr = (EmailAddressValue.value email)
        try
            Ok(AccountValidation.generateAccountTokenString emailStr utcNow encrypt TokenType.ResetPassword)
        with ex -> createInvalidErrorWithExn "email" "passwordResetToken.unexpectedError" ex

    let createEmailValidationToken utcNow encrypt (req: RegisterUserRequest) =
        let email = (EmailAddressValue.value req.Email)
        try
            Ok(AccountValidation.generateAccountTokenString email utcNow encrypt TokenType.ValidateEmail)
        with ex -> createInvalidErrorWithExn "email" "emailValidationToken.unexpectedError" ex

    let verifyPassword passwordHasher passwordValue (entity: UserRegistration) =
        let verifyResult = verifyHashedPassword passwordHasher entity.Password (DomainTypes.PasswordValue.value passwordValue)
        match verifyResult with
        | Ok isCorrectPwd -> Ok(isCorrectPwd, entity)
        | Error exn -> createInvalidErrorWithExn "password" "verifyPassword.unexpectedError" exn

    let hashPassword passwordHasher password =
        let hashResult = hashPassword passwordHasher password
        match hashResult with
        | Ok newPassword -> Ok(newPassword)
        | Error exn -> createInvalidErrorWithExn "password" "hashPassword.unexpectedError" exn

    let createNewUser utcNow withEmailActivation passwordHasher repository encrypt (req: RegisterUserRequest) =

        let emailValidationToken =
            (if (withEmailActivation) then Result.map Some (createEmailValidationToken utcNow encrypt req) else Ok(None))

        let tokenAndPasswordRes = (hashPassword passwordHasher (PasswordValue.value req.PasswordValue)) .<|>. emailValidationToken

        let entityResult =
            (fun (pwd, withValidationTokenOption) ->
                match (withValidationTokenOption) with
                | Some validationToken ->
                    UserRegistration
                        ((EmailAddressValue.value req.Email),
                         pwd,
                         StringRequiredValue.value req.Firstname,
                         StringRequiredValue.value req.Lastname,
                         validationToken,
                         utcNow,
                         Nullable utcNow,
                         AccountValidationStatus.Pending |> int,
                         Nullable utcNow)
                | None ->
                    UserRegistration
                        ((EmailAddressValue.value req.Email),
                         pwd,
                         StringRequiredValue.value req.Firstname,
                         StringRequiredValue.value req.Lastname,
                         String.Empty,
                         utcNow,
                         Nullable utcNow,
                         AccountValidationStatus.None |> int,
                         Nullable utcNow))
            <!> tokenAndPasswordRes

        entityResult .=> insertNewUser repository

    let createSystemFeatureTogglesFromString (input: String) =
        if String.IsNullOrWhiteSpace(input) then
            []
        else
            Seq.toList (input.ToUpperInvariant().Split(';', StringSplitOptions.RemoveEmptyEntries))

    let private createRegisterUserResponse jwtSecretKey utcNow (entity: UserRegistration) =

        // a newly registered used has no organisation yet
        let jwtToken = createJWT entity.Email "" [] [] utcNow jwtSecretKey

        Result.map (fun jwt ->
            {
                RegisterUserResp.email = entity.Email
                firstname = entity.Firstname
                lastname = entity.Lastname
                createdTimestamp = entity.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                lastModifiedTimestamp = entity.Timestamp.ToUnixTimeMilliseconds()
                accountValidationStatus = enum<AccountValidationStatus> entity.AccountValidationStatus
                witActivationMail = false
                activationMailResult = String.Empty
                JWT = jwt
            },
            entity.EmailValidationToken) jwtToken

    let private createAuthenticateUserResponse jwtSecretKey utcNow (entity: UserRegistration) =

        let jwtToken = createJWT entity.Email "" [] [] utcNow jwtSecretKey

        Result.map (fun jwt ->
            {
                AuthenticateUserResp.email = entity.Email
                firstname = entity.Firstname
                lastname = entity.Lastname
                systemFeatureToggles = createSystemFeatureTogglesFromString entity.SystemFeatureToggles
                createdTimestamp = entity.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                lastModifiedTimestamp = entity.Timestamp.ToUnixTimeMilliseconds()
                accountValidationStatus = enum<AccountValidationStatus> entity.AccountValidationStatus
                JWT = jwt
            }) jwtToken

    let registerUser
        utcNow
        passwordHasher
        repository
        encryptFunc
        jwtSecretKey
        withEmailActivation
        emailService
        emailTemplates
        buildActivationUrlFunc
        (logger: ILogger)
        (req: RegisterUserRequest)
        =

        let errorWhenUserNotExists x exists =
            if (exists) then createInvalidError "email" ("userAlreadyExists" :: []) else Ok(x)

        let responseRes =
            (userExist req.Email)
            >=> errorWhenUserNotExists req
            >=> createNewUser utcNow withEmailActivation passwordHasher repository encryptFunc
            >=> createRegisterUserResponse jwtSecretKey utcNow


        match (responseRes repository) with
        | Ok (response, emailToken) ->
            if (withEmailActivation) then

                let createEmailResult =
                    (emailTemplates.CreateUserRegistrationActivationMail
                        response.email
                         (buildActivationUrlFunc emailToken))

                let createAndSendResult =
                    createEmailResult.AndThen(fun emailToSend -> toErrorKindResult (sendEmail emailService emailToSend))

                match createAndSendResult with
                | Ok statusCode ->
                    Ok({ response with witActivationMail = true; activationMailResult = statusCode.ToString() })
                | Error errList ->
                    let errLogString = Log.logErrors logger Log.Events.SendEmailErrorEvent errList
                    Ok({ response with witActivationMail = true; activationMailResult = errLogString })
            else
                Ok(response)
        | Error e -> Error e


    let authenticateLoginUser utcNow passwordHasher userRepository jwtSecretKey (req: AuthenticateUserRequest) =

        let errorWhenUserNotExists x exists = if not (exists) then createInvalidError "password" ([]) else Ok(x)

        let responseRes =
            (userExist req.Email)
            >=> errorWhenUserNotExists req.Email
            >=> getUserByEmail userRepository
            >=> verifyPassword passwordHasher req.PasswordValue

        match (responseRes userRepository) with
        | Ok (isValidPassword, entity) ->
            if (isValidPassword) then
                createAuthenticateUserResponse jwtSecretKey utcNow entity
            else
                createInvalidError "password" ([])
        | Error e -> Error e

    let confirmUserEmail utcNow (repository: IUserRepository) jwtSecretKey decryptFunc (token: Token) =

        let compareWithStoredToken token =
            let getUserRes = getUserByEmail repository token.ForEmail
            getUserRes
            .=> (fun entity ->
                if entity.AccountValidationStatus = (int <| AccountValidationStatus.Pending) then
                    let tokenRes = AccountValidation.createToken entity.EmailValidationToken utcNow decryptFunc
                    tokenRes
                    .=> (fun tokenFromDb ->
                        match tokenFromDb with
                        | ValidToken tokenInfoFromDb ->
                            if tokenInfoFromDb = token then
                                Ok entity
                            else
                                createErrorWithErrInfo InvalidToken "token" ("differentFromStoredToken" :: [])
                        | ExpiredToken _ ->
                            createErrorWithErrInfo InvalidToken "token" ("expired.storedTokenExpired" :: []))
                else
                    createErrorWithErrInfo InvalidToken "token" ("userAccountAlreadyValidated" :: []))

        let updateUserToValidatedAccount repository utcNow (entity: UserRegistration) =
            entity.AccountValidationStatus <- int <| AccountValidationStatus.Validated
            entity.ValidationStatusUpdateDateTimeUtc <- Nullable utcNow
            entity.EmailValidationToken <- String.Empty
            updateUser repository utcNow entity

        match token with
        | ValidToken tokenInfo ->
            compareWithStoredToken tokenInfo
            .=> updateUserToValidatedAccount repository utcNow
            .=> createAuthenticateUserResponse jwtSecretKey utcNow
        | ExpiredToken _ -> createErrorWithErrInfo InvalidToken "token" ("expired" :: [])

    let forgotPassword
        utcNow
        repository
        encryptFunc
        emailService
        emailTemplates
        forgotPasswordUrlFunc
        (logger: ILogger)
        email
        =

        let createSendEmailRequest token =
            emailTemplates.CreateUserPasswordForgetMail (EmailAddressValue.value email) (forgotPasswordUrlFunc token)

        let existsResult = userExist email repository

        let updateUserWithPasswordResetToken utcNow repository email token: Result<string, ErrorKind> =
            getUserByEmail repository email
            .=> (fun user ->
                user.PasswordResetToken <- token
                user.PasswordResetTokenUpdateDateTimeUtc <- Nullable utcNow
                (fun _ -> token) <!> (updateUser repository utcNow user))


        match existsResult with
        | Ok userExists ->
            if not (userExists) then
                Ok(ignore userExists)
            else

                let createTokenResult = createPasswordResetToken utcNow encryptFunc email

                let runResult =
                    (toListOfError createTokenResult)
                    .=> (fun tokenStr ->
                        toListOfError (updateUserWithPasswordResetToken utcNow repository email tokenStr))
                    .=> createSendEmailRequest
                    .=> (fun emailToSend -> toErrorKindResult (sendEmail emailService emailToSend))

                match runResult with
                | Ok statusCode -> Ok(ignore statusCode)
                | Error errList ->
                    Log.logErrors logger Log.Events.SendEmailErrorEvent errList |> ignore
                    Error errList
        | Error err -> Error(err :: [])


    let resetPassword utcNow passwordHasher repository decryptFunc (req: ResetPasswordRequest) =

        match req.Token with
        | ValidToken tokenInfoFromReq ->
            getUserByEmail repository tokenInfoFromReq.ForEmail
            .=> (fun userEntity ->
                AccountValidation.createToken userEntity.PasswordResetToken utcNow decryptFunc
                .=> (fun tokenFromDb ->
                    match tokenFromDb with
                    | ValidToken tokenInfoFromDb ->
                        if tokenInfoFromReq = tokenInfoFromDb then

                            let passwordValidationResult =
                                DomainTypes.PasswordValue.create
                                    req.NewPasswordInput
                                    (Some
                                        {
                                            DomainTypes.ErrInfo.FieldName = "newPassword"
                                            DomainTypes.ErrInfo.Details = None
                                        })
                                    userEntity.Email
                                    userEntity.Firstname
                                    userEntity.Lastname

                            let newHashedPasswordRes =
                                passwordValidationResult
                                .=> (fun validatedPw -> hashPassword passwordHasher (PasswordValue.value validatedPw))

                            newHashedPasswordRes
                            .=> (fun hashedPassword ->
                                userEntity.Password <- hashedPassword
                                userEntity.PasswordResetToken <- String.Empty
                                userEntity.PasswordResetTokenUpdateDateTimeUtc <- Nullable utcNow

                                //When the account was not yet validated, set the account to validated,
                                //since the reset password has also confirmed the email
                                if userEntity.AccountValidationStatus <> (int <| AccountValidationStatus.Validated) then
                                    userEntity.AccountValidationStatus <- int <| AccountValidationStatus.Validated
                                    userEntity.ValidationStatusUpdateDateTimeUtc <- Nullable utcNow
                                    userEntity.EmailValidationToken <- String.Empty
                                    Ok userEntity
                                else
                                    Ok userEntity)
                            .=> updateUser repository utcNow
                            .=> (fun updatedEntity -> Ok(ignore updatedEntity))
                        //Ok unit
                        else
                            createErrorWithErrInfo InvalidToken "token" ("differentFromStoredToken" :: [])
                    | ExpiredToken _ -> createErrorWithErrInfo InvalidToken "token" ("expired.storedTokenExpired" :: [])))
        | ExpiredToken _ -> createErrorWithErrInfo InvalidToken "token" ("expired" :: [])

    let organisationExists orgName (repository: IOrganisationRepository) =
        try
            //Check if the user already exists
            Ok(repository.OrganisationExists((StringRequiredValue.value orgName)))
        with ex -> createInvalidErrorWithExn "OrganisationName" "organisationExists.unexpectedError" ex

    let getOrganisationById (repository: IOrganisationRepository) orgId =
        try
            let entity = repository.GetOrganisationById(StringRequiredValue.value orgId)
            if (not (entity = null)) then
                Ok(entity)
            else
                createInvalidError "organisationId" ("getOrganisationById.doesNotExist" :: [])
        with ex -> createInvalidErrorWithExn "organisationId" "getOrganisationById.unexpectedError" ex

    let insertNewOrganisation (repository: IOrganisationRepository) orgEntity =
        try
            let createdEntity = repository.InsertNewOrganisation orgEntity
            if (createdEntity = null) then
                createInvalidError "organisationName" ("insertNewOrganisation.errorGotNullResult" :: [])
            else
                Ok(createdEntity)
        with ex -> createInvalidErrorWithExn "organisationName" "insertNewOrganisation.unexpectedError" ex

    let addUserToOrganisation utcNow (repository: IOrganisationRepository) userId role (orgEntity: Organisation) =
        try
            repository.AddUserToOrganisation(EmailAddressValue.value userId, orgEntity.RowKey, role, utcNow)
            let orgsForUser = repository.GetOrganisationsForUser(EmailAddressValue.value userId)
            if (orgsForUser = null) then
                createInvalidError
                    "organisationName"
                    ("addUserToOrganisation.getOrganisationsForUser.errorGotNullResult" :: [])
            else
                Ok(orgEntity, orgsForUser)
        with ex -> createInvalidErrorWithExn "organisationName" "addUserToOrganisation.unexpectedError" ex

    let createUserOrganisationsApiTypeUnsafe userOrgsEntities getOrganisation =
        [

            for (userOrg: UserOrganisation) in userOrgsEntities do
                let (orgEntityForCurrent: Organisation) = getOrganisation userOrg.RowKey
                {
                    ApiTypes.UserOrganisation.id = userOrg.RowKey
                    ApiTypes.UserOrganisation.userRole = userOrg.Role
                    ApiTypes.UserOrganisation.userJoinedTimestamp =
                        userOrg.JoinedCreationDateTimeUtc.ToUnixTimeMilliseconds()
                    ApiTypes.UserOrganisation.name = orgEntityForCurrent.OrganisationName
                    ApiTypes.UserOrganisation.createdTimestamp =
                        orgEntityForCurrent.CreationDateTimeUtc.ToUnixTimeMilliseconds()
                    ApiTypes.UserOrganisation.lastModifiedTimestamp =
                        orgEntityForCurrent.LastModifiedDateTimeUtc.ToUnixTimeMilliseconds()
                    ApiTypes.UserOrganisation.createdByUserId = orgEntityForCurrent.CreatedByUserId
                }
        ]

    let createUserApiTypeUnsafe (userRegistrationEntity: UserRegistration) userOrganisationsApiType =
        {
            User.id = userRegistrationEntity.RowKey
            User.email = userRegistrationEntity.Email
            User.firstname = userRegistrationEntity.Firstname
            User.lastname = userRegistrationEntity.Lastname
            User.createdTimestamp = userRegistrationEntity.CreationDateTimeUtc.ToUnixTimeMilliseconds()
            User.lastModifiedTimestamp = userRegistrationEntity.LastModifiedDateTimeUtc.ToUnixTimeMilliseconds()
            User.accountValidationStatus = enum<AccountValidationStatus> userRegistrationEntity.AccountValidationStatus
            User.organisations = userOrganisationsApiType
            systemFeatureToggles = createSystemFeatureTogglesFromString userRegistrationEntity.SystemFeatureToggles
        }

    let createUserApiTypeForCreateOrg
        (userRegistrationEntity: UserRegistration)
        (initialOrgEntity: Organisation)
        (userOrgsEntities)
        (orgRepo: IOrganisationRepository)
        =
        try
            let getOrganisation orgId =
                if String.Equals(orgId, initialOrgEntity.RowKey, StringComparison.Ordinal) then
                    initialOrgEntity
                else
                    orgRepo.GetOrganisationById orgId

            let userOrganisationsApiType = createUserOrganisationsApiTypeUnsafe userOrgsEntities getOrganisation

            let userApiType = createUserApiTypeUnsafe userRegistrationEntity userOrganisationsApiType

            Ok userApiType
        with ex -> createInvalidErrorWithExn "organisationName" "createUserApiTypeForCreateOrg.unexpectedError" ex

    let createOrganisationForUser
        utcNow
        userRepository
        (orgRepository: IOrganisationRepository)
        (req: AddOrganisationRequest)
        =
        getUserByEmail userRepository req.UserId
        .<|>. organisationExists req.OrganisationName orgRepository
        .=> (fun (userRegistration, orgExists) ->
            if orgExists then
                createErrorWithErrInfo Invalid "organisationName" ("alreadyExists" :: [])
            else
                let orgEntity =
                    Organisation
                        (StringRequiredValue.value req.OrganisationName, EmailAddressValue.value req.UserId, utcNow)

                insertNewOrganisation orgRepository orgEntity
                .=> addUserToOrganisation utcNow orgRepository req.UserId "Owner"
                .=> (fun (org, userOrgs) -> createUserApiTypeForCreateOrg userRegistration org userOrgs orgRepository))

    let addUserToOrganisationWithRole
        utcNow
        userRepository
        (orgRepository: IOrganisationRepository)
        userId
        organisationId
        role
        =
        getUserByEmail userRepository userId
        .<|>. getOrganisationById orgRepository organisationId
        .=> (fun (userRegistration, orgEntity) ->
            addUserToOrganisation utcNow orgRepository userId role orgEntity

            .=> (fun (org, userOrgs) -> createUserApiTypeForCreateOrg userRegistration org userOrgs orgRepository))

    let getUserInfo utcNow userRepository (orgRepository: IOrganisationRepository) userId =
        getUserByEmail userRepository userId
        .=> (fun userEntity ->
            try
                let orgsForUser = orgRepository.GetOrganisationsForUser(userEntity.RowKey)
                let orgsApiType = createUserOrganisationsApiTypeUnsafe orgsForUser orgRepository.GetOrganisationById
                Ok(createUserApiTypeUnsafe userEntity orgsApiType)
            with ex -> createInvalidErrorWithExn "userId" "getUserInfo.unexpectedError" ex)

    let updateUserInfo
        utcNow
        (userRepository: IUserRepository)
        (orgRepository: IOrganisationRepository)
        (req: UpdateUserRequest)
        =
        getUserByEmail userRepository req.UserId
        .=> (fun userEntity ->
            try
                userEntity.Firstname <- (StringRequiredValue.value req.Firstname)
                userEntity.Lastname <- (StringRequiredValue.value req.Lastname)

                let updatedUserEntity = userRepository.UpdateUser(userEntity, Nullable utcNow)
                let orgsForUser = orgRepository.GetOrganisationsForUser(updatedUserEntity.RowKey)
                let orgsApiType = createUserOrganisationsApiTypeUnsafe orgsForUser orgRepository.GetOrganisationById
                Ok(createUserApiTypeUnsafe userEntity orgsApiType)
            with ex -> createInvalidErrorWithExn "userId" "updateUserInfo.unexpectedError" ex)

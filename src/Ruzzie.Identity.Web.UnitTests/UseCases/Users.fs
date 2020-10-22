module Ruzzie.Identity.Web.UnitTests.UseCases.Users

open System.Collections.ObjectModel
open FluentAssertions
open System
open System.Collections.Generic
open System.Net
open Ruzzie.Identity.Web
open DomainTypes.AccountValidation
open Authentication.JWT
open Email
open ResultLib
open Microsoft.Extensions.Logging
open NUnit.Framework
open Ruzzie.Identity.Storage.Azure
open Ruzzie.Identity.Storage.Azure.Entities
open Ruzzie.Identity.Web.ApiTypes
open UseCases.Shared
open Validation

type UserRepositoryTestStub(userExists: string -> bool, insertNewUser: UserRegistration -> UserRegistration, getUserByEmail: string -> UserRegistration, updateUser: UserRegistration -> DateTimeOffset Nullable -> UserRegistration) =
    interface IUserRepository with
        member this.UserExists(email: string) = userExists email
        member this.InsertNewUser(entity: UserRegistration) = insertNewUser entity
        member this.GetUserByEmail(email: string) = getUserByEmail email
        member this.UpdateUser(entity: UserRegistration, utcNow: DateTimeOffset Nullable) = updateUser entity utcNow
        member this.DeleteUser(email: string) = ignore true

let validFakeAccountValidationToken email =
    AccountValidation.generateAccountTokenString email (DateTimeOffset.UtcNow) (fun e -> e)
        AccountValidation.TokenType.ValidateEmail

let validFakeOrgInviteToken email orgId =
    Organisations.generateOrganisationTokenString email orgId (DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero))
        (fun e -> e) Organisations.OrganisationTokenType.Invitation

let invitesReadOnlyList organisationId =
    let netList = List<Ruzzie.Identity.Storage.Azure.Entities.OrganisationInvite>()
    netList.Add
        (Ruzzie.Identity.Storage.Azure.Entities.OrganisationInvite
            (organisationId, "validinvitee@valid.org", "valid@valid.org",
             (validFakeOrgInviteToken "validinvitee@valid.org" organisationId),
             DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero), 2))
    netList :> IReadOnlyList<Ruzzie.Identity.Storage.Azure.Entities.OrganisationInvite>

type OrganisationRepositoryTestStub() =
    interface IOrganisationRepository with
        member this.OrganisationExists(organisationName: string) = false
        member this.UpsertOrganisationInvite(entity: Ruzzie.Identity.Storage.Azure.Entities.OrganisationInvite, utcNow: DateTimeOffset Nullable) = entity
        member this.GetOrganisationById(organisationId: string) =
            Organisation(organisationId, "valid@valid.org", DateTimeOffset.UtcNow)
        member this.DeleteOrganisation(organisationId: string) = ignore true
        member this.AddUserToOrganisation(userId: string, organisationId: string, role: string,
                                          joinedCreationDateTimeUtc: DateTimeOffset) = ignore true
        member this.UserIsInOrganisation(organisationId: string, userId: string) = false
        member this.DeleteUserFromOrganisation(userId: string, organisationId: string) = ignore false
        member this.DeleteOrganisationInvite(organisationId: string, userId: string) = ignore false
        member this.GetUsersForOrganisation(organisationId: string) =
            ReadOnlyCollection<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
                (List<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
                    ((Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser(organisationId, "valid@valid.org", "Default", DateTimeOffset.UtcNow) :: []))) :> IReadOnlyList<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
        member this.GetOrganisationsForUser(userId: string) =
            ReadOnlyCollection<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>
                (List<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>((Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation(userId, "FAKEORG", "Default", DateTimeOffset.UtcNow) :: []))) :> IReadOnlyList<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>
        member this.InsertNewOrganisation(entity: Ruzzie.Identity.Storage.Azure.Entities.Organisation) = entity
        member this.UpdateOrganisation(entity: Ruzzie.Identity.Storage.Azure.Entities.Organisation, utcNow: DateTimeOffset Nullable) = entity
        member this.GetOrganisationInvite(organisationId: string, userId: string) =
            OrganisationInvite
                (organisationId, userId, "valid@valid.org", (validFakeOrgInviteToken userId organisationId),
                 new DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero), 2)
        member this.GetAllOrganisationInvites(organisationId: string, invitationStatus: int) =
            invitesReadOnlyList organisationId

        member this.GetAllOrganisationIds() = List<string>() :> IReadOnlyList<string>


//let validFakePasswordResetToken email =
//    AccountValidation.generateAccountTokenString email (DateTimeOffset.UtcNow) (fun e -> e) TokenType.ResetPassword

let emptyDefaultUserRepositoryStub =
    UserRepositoryTestStub
        ((fun _ -> false), (fun entity -> entity),
         (fun email ->
             UserRegistration
                 (email, "defaultpass", "fake", "stub", validFakeAccountValidationToken email, DateTimeOffset.UtcNow)),
         (fun entity utcNow -> entity))

let userRepoForPassword password =
    UserRepositoryTestStub
        ((fun _ -> true), (fun entity -> entity),
         (fun email ->
             UserRegistration
                 (email, password, "fake", "stub", validFakeAccountValidationToken email, DateTimeOffset.UtcNow)),
         (fun entity utcNow -> entity))

let userRepoForPasswordAndPasswordResetToken password passwordResetToken =
    UserRepositoryTestStub
        ((fun _ -> true), (fun entity -> entity),
         (fun email ->
             UserRegistration
                 (email, password, "fake", "stub", validFakeAccountValidationToken email, DateTimeOffset.UtcNow,
                  passwordResetToken = passwordResetToken)), (fun entity utcNow -> entity))

let userRepoForAccountToken token accountStatus =
    UserRepositoryTestStub
        ((fun _ -> true), (fun entity -> entity),
         (fun email ->
             UserRegistration
                 (email, "defaultpass", "fake", "stub", token, DateTimeOffset.UtcNow,
                  accountValidationStatus = accountStatus)), (fun entity utcNow -> entity))

let jwtTestConfig =
    { JWTConfig.Secret = "psssssssssssssssssssssssssssssssssssssssssssssht"
      ExpirationInMinutes = 5
      Issuer = "test"
      Audience = "testA" }

let logger<'a> = Microsoft.Extensions.Logging.Logger(LoggerFactory.Create((fun f -> f |> ignore)))
let emailTemplates = {   CreateUserRegistrationActivationMail = Templates.userRegistrationActivationMail
                         CreateUserPasswordForgetMail = Templates.userPasswordForgetMail
                         CreateInviteUserToOrganisationEmail = Templates.inviteUserToOrganisationEmail }

let registerValidUserWithEmailService req emailService =
    let register validInput =
        UseCases.Users.registerUser DateTimeOffset.UtcNow emptyDefaultUserRepositoryStub (fun e -> e) jwtTestConfig
            true emailService emailTemplates
            (fun token -> UseCases.Shared.createUrlWithOneQuery "https" "localhost" "activate" ("token", token)) logger
            validInput |> toListOfError

    let run = RegisterUserInput.validateRegisterUserInput >=> register
    run req

let defaultEmptyEmailService = { EmailService.Send = (fun mail -> Ok HttpStatusCode.OK) }

let registerValidUser req =
    registerValidUserWithEmailService req defaultEmptyEmailService

[<Test>]
let ``register valid user `` () =
    //Arrange
    let req =
        { RegisterUserReq.email = "valid@valid.org"
          firstname = "Valid"
          lastname = "NoName"
          password = "very long test password for this test" }

    //Act
    let result = registerValidUser req

    //Assert
    match result with
    | Ok response -> response.email.Should().Be("valid@valid.org", null) |> ignore
    | Error errors -> Assert.Fail(errors.ToString())

[<Test>]
let ``register valid user should send activation-mail with link `` () =
    //Arrange
    let req =
        { RegisterUserReq.email = "valid@valid.org"
          firstname = "Valid"
          lastname = "NoName"
          password = "very long test password for this test" }

    //Act
    let result =
        registerValidUserWithEmailService req
            { EmailService.Send =
                  (fun mail ->
                      (StringNonEmpty.value mail.TextContent).Should().Contain("https://localhost/activate?", null)
                      |> ignore
                      Ok HttpStatusCode.OK) }

    //Assert
    match result with
    | Ok response -> response.email.Should().Be("valid@valid.org", null) |> ignore
    | Error errors -> Assert.Fail(errors.ToString())

[<Test>]
let ``register valid user and activation-mail send fails`` () =
    //Arrange
    let req =
        { RegisterUserReq.email = "valid@valid.org"
          firstname = "Valid"
          lastname = "NoName"
          password = "very long test password for this test" }

    //Act
    let result =
        registerValidUserWithEmailService req
            { EmailService.Send = (fun mail -> Error(HttpStatusCode.NotFound, "mail error response")) }

    //Assert
    match result with
    | Ok response ->
        response.activationMailResult.Should().Contain("[Unexpected]->(404): mail error response", null) |> ignore
    | Error errors -> Assert.Fail(errors.ToString())

[<Test>]
let ``authenticate valid user`` () =

    //Arrange
    let password =
        match Security.hashPassword "very long test password for this test" with
        | Ok pw -> pw
        | Error e -> e.ToString()

    let req =
        { AuthenticateUserReq.email = "valid@valid.org"
          password = "very long test password for this test" }

    let authenticate validInput =
        UseCases.Users.authenticateLoginUser DateTimeOffset.UtcNow (userRepoForPassword password) jwtTestConfig
            validInput |> toListOfError

    let run = AuthenticateUserInput.validateInput >=> authenticate

    //Act
    let result = run req

    //Assert
    match result with
    | Ok response -> response.email.Should().Be("valid@valid.org", null) |> ignore
    | Error errors -> Assert.Fail(errors.ToString())

[<Test>]
let ``authentication failed invalid password`` () =
    //Arrange
    let password =
        match Security.hashPassword "very long test password for this test" with
        | Ok pw -> pw
        | Error e -> e.ToString()

    let req =
        { AuthenticateUserReq.email = "valid@valid.org"
          password = "invalid password !" }

    let authenticate validInput =
        UseCases.Users.authenticateLoginUser DateTimeOffset.UtcNow (userRepoForPassword password) jwtTestConfig
            validInput |> toListOfError

    let run = AuthenticateUserInput.validateInput >=> authenticate

    //Act
    let result = run req

    //Assert
    match result with
    | Ok _ -> Assert.Fail("Expected error") |> ignore
    | Error errors ->
        match (List.head errors) with
        | Invalid e -> e.Value.FieldName.Value.Should().Be("password", null) |> ignore
        | _ -> Assert.Fail("Expected Other errorType")
    |> ignore

[<Test>]
let ``confirm user email with valid email token`` () =

    //Arrange
    let validationTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" DateTimeOffset.UtcNow (fun e -> e)
            TokenType.ValidateEmail
    let repo = userRepoForAccountToken validationTokenStr (int <| AccountValidationStatus.Pending) :> IUserRepository

    //Act
    let confirmResult =
        AccountValidation.createToken validationTokenStr DateTimeOffset.UtcNow (fun d -> d)
        .=> UseCases.Users.confirmUserEmail DateTimeOffset.UtcNow repo jwtTestConfig (fun d -> d)

    //Assert
    match confirmResult with
    | Ok resp -> resp.accountValidationStatus.Should().Be(AccountValidationStatus.Validated, null) |> ignore
    | Error err -> Assert.Fail(err.ToString())

[<Test>]
let ``confirm user email with expired email token`` () =

    //Arrange
    let validationTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org"
            (DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100.0))) (fun e -> e) TokenType.ValidateEmail
    let repo = userRepoForAccountToken validationTokenStr (int <| AccountValidationStatus.Pending) :> IUserRepository

    //Act
    let confirmResult =
        AccountValidation.createToken validationTokenStr DateTimeOffset.UtcNow (fun d -> d)
        .=> UseCases.Users.confirmUserEmail DateTimeOffset.UtcNow repo jwtTestConfig (fun d -> d)

    //Assert
    match confirmResult with
    | Ok resp -> Assert.Fail("Expected error but got: " + resp.ToString()) |> ignore
    | Error err ->
        match (err) with
        | InvalidToken info -> info.Value.Details.Value.Head.Should().Be("expired", null) |> ignore
        | _ -> Assert.Fail(err.ToString())

[<Test>]
let ``confirm user email with already validated email token`` () =

    //Arrange
    let validationTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" DateTimeOffset.UtcNow (fun e -> e)
            TokenType.ValidateEmail
    let repo =
        userRepoForAccountToken validationTokenStr (int <| AccountValidationStatus.Validated) :> IUserRepository

    //Act
    let confirmResult =
        AccountValidation.createToken validationTokenStr DateTimeOffset.UtcNow (fun d -> d)
        .=> UseCases.Users.confirmUserEmail DateTimeOffset.UtcNow repo jwtTestConfig (fun d -> d)

    //Assert
    match confirmResult with
    | Ok resp -> Assert.Fail("Expected error but got: " + resp.ToString()) |> ignore
    | Error err ->
        match (err) with
        | InvalidToken info -> info.Value.Details.Value.Head.Should().Be("userAccountAlreadyValidated", null) |> ignore
        | _ -> Assert.Fail(err.ToString())

[<Test>]
let ``forgot password for valid user`` () =
    //Act
    let runResult =
        EmailAddressValue.create "valid@valid.org" None
        |> toListOfError
        .=> UseCases.Users.forgotPassword DateTimeOffset.UtcNow (userRepoForPassword "lkasjdlaksjd ff323!@")
                (fun e -> e) defaultEmptyEmailService emailTemplates
                (fun token -> UseCases.Shared.createUrlWithOneQuery "https" "localhost" "reset" ("token", token))
                logger

    //Assert
    match runResult with
    | Ok _ -> ignore
    | Error errors ->
        Assert.Fail(errors.ToString())
        ignore

    |> ignore

[<Test>]
let ``reset password for valid user with valid new password`` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let resetTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" utcNow (fun e -> e) TokenType.ResetPassword
    let stubRepo = userRepoForPasswordAndPasswordResetToken "newValidPassword ff323!@" resetTokenStr

    //Act
    let runResult =
        AccountValidation.createToken resetTokenStr utcNow (fun d -> d)
        .=> (fun token ->
            UseCases.Users.resetPassword DateTimeOffset.UtcNow (stubRepo) (fun d -> d)
                { ResetPasswordRequest.Token = token
                  ResetPasswordRequest.NewPasswordInput = "newValidPassword ff323!@" })

    //Assert
    match runResult with
    | Ok _ -> ignore
    | Error errors ->
        Assert.Fail(errors.ToString())
        ignore

    |> ignore

[<Test>]
let ``reset password for valid user with valid new password should also validate the account`` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let resetTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" utcNow (fun e -> e) TokenType.ResetPassword

    let stubRepo =
     UserRepositoryTestStub
        ((fun _ -> true), (fun entity -> entity),
         (fun email ->
             UserRegistration
                 (email, "newValidPassword ff323!@", "fake", "stub", validFakeAccountValidationToken email, DateTimeOffset.UtcNow,
                  passwordResetToken = resetTokenStr)),
         (fun entity utcNow ->
                        Assert.AreEqual(entity.AccountValidationStatus, int <| AccountValidationStatus.Validated)
                        entity))


    //Act
    let runResult =
        AccountValidation.createToken resetTokenStr utcNow (fun d -> d)
        .=> (fun token ->
            UseCases.Users.resetPassword DateTimeOffset.UtcNow (stubRepo) (fun d -> d)
                { ResetPasswordRequest.Token = token
                  ResetPasswordRequest.NewPasswordInput = "newValidPassword ff323!@" })

    //Assert
    match runResult with
    | Ok _ -> ignore
    | Error errors ->
        Assert.Fail(errors.ToString())
        ignore

    |> ignore

[<Test>]
let ``reset password for valid user with invalid new password`` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let resetTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" utcNow (fun e -> e) TokenType.ResetPassword
    let stubRepo = userRepoForPasswordAndPasswordResetToken "newValidPassword ff323!@" resetTokenStr

    //Act
    let runResult =
        AccountValidation.createToken resetTokenStr utcNow (fun d -> d)
        .=> (fun token ->
            UseCases.Users.resetPassword DateTimeOffset.UtcNow (stubRepo) (fun d -> d)
                { ResetPasswordRequest.Token = token
                  ResetPasswordRequest.NewPasswordInput = "invalid" })

    //Assert
    match runResult with
    | Ok _ ->
        Assert.Fail("Expected error")
        ignore
    | Error _ -> ignore
    |> ignore

[<Test>]
let ``reset password for valid user with valid new password with expired token`` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let resetTokenStr =
        AccountValidation.generateAccountTokenString "valid@valid.org" (utcNow.Subtract(TimeSpan.FromDays(365.0)))
            (fun e -> e) TokenType.ResetPassword
    let stubRepo = userRepoForPasswordAndPasswordResetToken "newValidPassword ff323!@" resetTokenStr

    //Act
    let runResult =
        AccountValidation.createToken resetTokenStr utcNow (fun d -> d)
        .=> (fun token ->
            UseCases.Users.resetPassword DateTimeOffset.UtcNow (stubRepo) (fun d -> d)
                { ResetPasswordRequest.Token = token
                  ResetPasswordRequest.NewPasswordInput = "newValidPassword ff323!@" })

    //Assert
    match runResult with
    | Ok _ ->
        Assert.Fail("Expected error but got Ok")
        ignore
    | Error errKind ->
        match errKind with
        | InvalidToken _ -> ignore //expected!
        | _ ->
            Assert.Fail("Expected InvalidToken, but got" + errKind.ToString())
            ignore
    |> ignore

[<Test>]
let ``unregister user deletes user and user from organisation `` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let stubUserRepo = emptyDefaultUserRepositoryStub
    let stubOrgRepo = OrganisationRepositoryTestStub()

    //Act
    let runResult =
        EmailAddressValue.create "valid@valid.org" None |> toListOfError
        .<|>.
        (EmailAddressValue.create "valid@valid.org" None |> toListOfError)
        .=>
        (fun (requestedUserId, userId ) -> UseCases.Unregister.unregisterUser utcNow stubUserRepo stubOrgRepo requestedUserId userId)

    //Assert
    match runResult with
    | Ok _ -> ignore true
    | Error errors -> Assert.Fail(errors.ToString())
    |> ignore

[<Test>]
let ``valid user adds a valid new organisation`` () =
    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let stubUserRepo = emptyDefaultUserRepositoryStub
    let stubOrgRepo = OrganisationRepositoryTestStub()

    //Act
    let runResult =
        EmailAddressValue.create "valid@valid.org" None
        .=> AddOrganisationUserInput.validateInput { AddOrganisationReq.organisationName = "FAKEORG" }
        .=> UseCases.Users.createOrganisationForUser utcNow stubUserRepo stubOrgRepo

    //Assert
    match runResult with
    | Ok userApiType -> userApiType.organisations.Length.Should().Be(1, null) |> ignore
    | Error errors -> Assert.Fail(errors.ToString())
    |> ignore

[<Test>]
let ``inviteUserToOrganisation for valid user and org sends invite`` () =

    //Arrange
    let utcNow = DateTimeOffset.UtcNow
    let stubUserRepo = emptyDefaultUserRepositoryStub
    let stubOrgRepo = OrganisationRepositoryTestStub()

    let organisationId = "FAKEORG"
    let invitedByUserId = "valid@valid.org"
    let inviteeEmail = "invitee@valid.org"

    let createRequestResult =
        EmailAddressValue.create invitedByUserId None
        |> toListOfError
        .=> Validation.InviteUserToOrganisationInput.validateInput inviteeEmail organisationId

    //Act
    let runResult =
        createRequestResult
        .=> (fun req ->
            UseCases.Organisations.inviteUserToOrganisation utcNow stubUserRepo stubOrgRepo (fun e -> e)
                defaultEmptyEmailService emailTemplates (fun token -> "http://test?" + token) logger req |> toListOfError)

    //Assert
    match runResult with
    | Ok str -> ignore str
    | Error err -> Assert.Fail(err.ToString()) |> ignore


type OrganisationRepositoryMock(getOrganisationInvite, organisationExists, userIsInOrganisation) =
    interface IOrganisationRepository with
        member this.OrganisationExists(organisationName: string) = organisationExists organisationName
        member this.UpsertOrganisationInvite(entity: Ruzzie.Identity.Storage.Azure.Entities.OrganisationInvite, utcNow: DateTimeOffset Nullable) = entity
        member this.GetOrganisationById(organisationId: string) =
            Organisation(organisationId, "valid@valid.org", DateTimeOffset.UtcNow)
        member this.DeleteOrganisation(organisationId: string) = ignore true
        member this.AddUserToOrganisation(userId: string, organisationId: string, role: string,
                                          joinedCreationDateTimeUtc: DateTimeOffset) = ignore true
        member this.DeleteUserFromOrganisation(userId: string, organisationId: string) = ignore false
        member this.DeleteOrganisationInvite(organisationId: string, userId: string) = ignore false
        member this.UserIsInOrganisation(organisationId: string, userId: string) =
            userIsInOrganisation organisationId userId
        member this.GetUsersForOrganisation(organisationId: string) =
            ReadOnlyCollection<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
                (List<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
                    ((Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser(organisationId, "valid@valid.org", "Default", DateTimeOffset.UtcNow) :: []))) :> IReadOnlyList<Ruzzie.Identity.Storage.Azure.Entities.OrganisationUser>
        member this.GetOrganisationsForUser(userId: string) =
            ReadOnlyCollection<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>
                (List<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>((Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation(userId, "FAKEORG", "Default", DateTimeOffset.UtcNow) :: []))) :> IReadOnlyList<Ruzzie.Identity.Storage.Azure.Entities.UserOrganisation>
        member this.InsertNewOrganisation(entity: Ruzzie.Identity.Storage.Azure.Entities.Organisation) = entity
        member this.UpdateOrganisation(entity: Ruzzie.Identity.Storage.Azure.Entities.Organisation, utcNow: DateTimeOffset Nullable) = entity
        member this.GetOrganisationInvite(organisationId: string, userId: string) =
            getOrganisationInvite organisationId userId
        member this.GetAllOrganisationInvites(organisationId: string, invitationStatus: int) =
            invitesReadOnlyList organisationId
        member this.GetAllOrganisationIds() = List<string>() :> IReadOnlyList<string>


[<Test>]
let ``accept userInvitation for valid user`` () =

    //Arrange
    let organisationId = "FAKEORG"
    let invitedByUserId = "valid@valid.org"
    let inviteeEmail = "invitee@valid.org"

    let utcNow = DateTimeOffset.UtcNow
    let stubUserRepo = emptyDefaultUserRepositoryStub
    let inviteTokenStr = (validFakeOrgInviteToken inviteeEmail organisationId)
    let stubOrgRepo =
        OrganisationRepositoryMock
            ((fun organisationId userId ->
                OrganisationInvite
                    (organisationId, userId, invitedByUserId, inviteTokenStr,
                     DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero), 2)), (fun s -> false), (fun s x -> true))


    let request =
        { firstname = "invitee"
          lastname = "valid"
          password = "very long and cool password from user"
          invitationToken = inviteTokenStr }

    //Act
    let runResult =
        UseCases.Organisations.acceptOrganisationInvite utcNow stubUserRepo stubOrgRepo (fun d -> d) (fun e -> e) jwtTestConfig
            true defaultEmptyEmailService emailTemplates (fun u -> u) logger request

    //Assert
    match runResult with
    | Ok resp -> ignore resp
    | Error err -> Assert.Fail(err.ToString()) |> ignore

[<Test>]
let ``get all users for organisation`` () =
    //Arrange
    let organisationId = "FAKEORG"
    let userId = "valid@valid.org"
    let invitedByUserId = "valid@valid.org"
    let inviteeEmail = "invitee@valid.org"

    let utcNow = DateTimeOffset.UtcNow
    let stubUserRepo = emptyDefaultUserRepositoryStub
    let inviteTokenStr = (validFakeOrgInviteToken inviteeEmail organisationId)

    let stubOrgRepo =
        OrganisationRepositoryMock
            ((fun organisationId userId ->
                OrganisationInvite
                    (organisationId, userId, invitedByUserId, inviteTokenStr,
                     DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero), 2)), (fun _orgId -> true),
             (fun _orgId _userId -> true))

    //Act
    let runResult =
        EmailAddressValue.create userId None
        |> toListOfError
        .=> Validation.GetAllUsersForOrganisationInput.validateInput organisationId
        .=> (fun req ->
            UseCases.Organisations.getAllUsersForOrganisation utcNow stubUserRepo stubOrgRepo req |> toListOfError)

    //Assert
    match runResult with
    | Ok org -> Assert.AreEqual(org.id, organisationId) |> ignore
    | Error errors -> Assert.Fail(errors.ToString()) |> ignore

[<Test>]
let ``delete user from organisation`` () =
    //Arrange
    let organisationId = "FAKEORG"
    let requestedBy = "valid@valid.org"
    let userId = "user@valid.org"
    let stubOrgRepo =
        OrganisationRepositoryMock
            ((fun organisationId userId ->
                OrganisationInvite
                    (organisationId, userId, requestedBy, "ignore", DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero),
                     2)), (fun _orgId -> true), (fun _orgId _userId -> true))


    //Act
    let runResult =
        EmailAddressValue.create requestedBy None
        |> toListOfError
        .=> Validation.DeleteUserFromOrganisationInput.validateInput organisationId userId
        .=> (fun req -> UseCases.Organisations.deleteUserFromOrganisation stubOrgRepo req |> toListOfError)

    //Assert
    match runResult with
    | Ok r -> ignore r
    | Error errors -> Assert.Fail(errors.ToString()) |> ignore

[<Test>]
let ``delete user invite from organisation`` () =
    //Arrange
    let organisationId = "FAKEORG"
    let requestedBy = "valid@valid.org"
    let userId = "user@valid.org"
    let stubOrgRepo =
        OrganisationRepositoryMock
            ((fun organisationId userId ->
                OrganisationInvite
                    (organisationId, userId, requestedBy, "ignore", DateTimeOffset(2020, 4, 22, 1, 0, 0, TimeSpan.Zero),
                     2)), (fun _orgId -> true), (fun _orgId _userId -> true))

    //Act
    let runResult =
        EmailAddressValue.create requestedBy None
        |> toListOfError
        .=> Validation.DeleteUserFromOrganisationInput.validateInput organisationId userId
        .=> (fun req -> UseCases.Organisations.deleteUserInviteFromOrganisation stubOrgRepo req |> toListOfError)

    //Assert
    match runResult with
    | Ok r -> ignore r
    | Error errors -> Assert.Fail(errors.ToString()) |> ignore
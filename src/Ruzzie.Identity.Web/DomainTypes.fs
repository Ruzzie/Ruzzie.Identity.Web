namespace Ruzzie.Identity.Web

open System
open ResultLib
open Ruzzie.Common.Validation

[<AutoOpen>]
module DomainTypes =

    type ErrInfo =
        { FieldName: String option //TODO MAKE NON OPTIONAL
          Details: String list option }

    type ErrorKind =
        | CannotBeEmpty of ErrInfo option
        | Invalid of ErrInfo option
        | TooLong of ErrInfo option
        | TooShort of ErrInfo option
        | InvalidToken of ErrInfo option
        | Unexpected of (int * string)
        | Unauthorized

    let createErrKindWithErrInfo errKind fieldName details =
        errKind
                ((Some
                    { ErrInfo.FieldName = Some fieldName
                      ErrInfo.Details = Some(details) }))
    let createErrorWithErrInfo errKind fieldName details  =
         Error
            (createErrKindWithErrInfo errKind fieldName details)

    let createInvalidError fieldName details =
        createErrorWithErrInfo Invalid fieldName details

    let createInvalidErrorWithExn fieldName detail (exn: Exception) =
        createInvalidError fieldName (detail :: exn.Message :: [])

    let createInvalidErrKindWithExnOpt fieldName detail (exn: Exception option)  =
        match exn with
        | Some ex -> createErrKindWithErrInfo Invalid fieldName (detail :: ex.Message :: [])
        | None  -> createErrKindWithErrInfo Invalid fieldName (detail :: [])

    let detailsOrEmptyString errInfoOption =
        match errInfoOption with
        | None -> ""
        | Some info ->
            match info.Details with
            | Some details -> details |> List.fold (fun str curr -> str + "." + curr) ""
            | None -> ""

    let fieldNameOrGeneric errInfoOption =
        match errInfoOption with
        | Some info ->
            match info.FieldName with
            | Some fieldName -> fieldName
            | None -> "generic"
        | None -> "generic"

    let toErrorMessage errType errInfoOption =
        sprintf "error.%s%s" (errType.GetType().Name) (detailsOrEmptyString errInfoOption)

    let toLogString error =

        let str field errMsg =
            sprintf "[%s]->%s" field errMsg
        let errToStr error errInfoOption =
            str (fieldNameOrGeneric errInfoOption) (toErrorMessage error errInfoOption)

        match error with
        | TooShort errInfoOption ->
            errToStr error errInfoOption
        | TooLong errInfoOption ->
            errToStr error errInfoOption
        | Invalid errInfoOption ->
            errToStr error errInfoOption
        | CannotBeEmpty errInfoOption ->
            errToStr error errInfoOption
        | InvalidToken errInfoOption ->
            errToStr error errInfoOption
        | Unexpected (status, message) ->
            str ("Unexpected") ("("+status.ToString() + "): " +  message)
        | Unauthorized ->
            str "Unauthorized" String.Empty

    module EmailAddressValue =

        type T = private ValidEmailAddress of string

        let regularizeValidEmailAddress emailAddress =
            match emailAddress with
            | ValidEmailAddress emailStr -> ValidEmailAddress(emailStr.Trim().ToLower())

        let create email errInfoOption =
            if String.IsNullOrWhiteSpace email then Error(CannotBeEmpty(errInfoOption))
            else if EmailValidation.IsValidEmailAddress email = false then Error(Invalid(errInfoOption))
            else Ok(regularizeValidEmailAddress (ValidEmailAddress email))

        let value (emailValue) =
            match emailValue with
            | ValidEmailAddress s -> s

    module StringRequiredValue =

        type T = private T of string

        let create str errInfoOption =
            if String.IsNullOrWhiteSpace str then Error(CannotBeEmpty(errInfoOption))
            else if (String.IsNullOrWhiteSpace(str.Trim())) then Error(CannotBeEmpty(errInfoOption))
            else if (str.Length > Constants.Size32K) then Error(TooLong(errInfoOption))
            else Ok(T str)

        let value (T str) = str

    module StringNonEmpty =
        type T = private T of string
        let create str errInfoOption =
            if String.IsNullOrWhiteSpace str then Error(CannotBeEmpty(errInfoOption))
            else if (String.IsNullOrWhiteSpace(str.Trim())) then Error(CannotBeEmpty(errInfoOption))
            else Ok(T str)
        let value (T str) = str

    module PasswordValue =

        type T = private T of StringRequiredValue.T

        type PasswordOptions =
            { MinLength: int
              MaxLength: int
              }

        let defaultPasswordOptions =
            { MinLength = 10
              MaxLength = 128}

        let pwdLengthValidation errInfoOption requiredStr =
            let value = StringRequiredValue.value requiredStr
            match value.Length with
            | l when l < defaultPasswordOptions.MinLength -> Error(TooShort(errInfoOption))
            | l when l > defaultPasswordOptions.MaxLength -> Error(TooLong(errInfoOption))
            | _ -> Ok(requiredStr)

        let containsOnlyDigits str = String.forall Char.IsNumber str
        let containsAnySymbol str = not (String.exists Char.IsLetterOrDigit str)
        let containsMixedCase str = (String.exists Char.IsLower str) && (String.exists Char.IsUpper str)
        let containsAnyNumber str = String.exists Char.IsNumber str

        //todo make function names a bit clearer in context
        let startsWithPartialStrWhenGtOne (str: String) part =
            if not (String.IsNullOrWhiteSpace(part)) then
                if  part.Length = 1 then
                    false
                else
                    str.StartsWith(part.Substring(0, (Math.Min(part.Length, 4))), StringComparison.OrdinalIgnoreCase)
            else true

        let containsPartialStrWhenGtOne (input: String) part =
            if not (String.IsNullOrWhiteSpace(part)) then
                if  part.Length = 1 then
                    false
                else
                    input.Contains(part.Substring(0, (Math.Min(part.Length, 6))), StringComparison.OrdinalIgnoreCase)
            else true

        let addErrDetails errInfoOption detailToAdd =
            match errInfoOption with
            | Some errInfo ->
                match errInfo.Details with
                | Some details -> { errInfo with Details = Some(detailToAdd :: details) }
                | None -> { errInfo with Details = Some(detailToAdd :: []) }
            | None ->
                { FieldName = None
                  Details = Some(detailToAdd :: []) }

        let pwdStrengthValidation errInfoOption (forEmail: String) (forFirstname: String) (forLastname: String) requiredStr =
            let value = StringRequiredValue.value requiredStr

            match value with
            | s when containsOnlyDigits s ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotContainsOnlyNumbers"))))
            | s when value.Length < 12 && not (containsAnySymbol s) ->
                Error(Invalid((Some(addErrDetails errInfoOption "ShortPasswordsMustContainSymbols"))))
            | s when value.Length < 16 && not (containsAnyNumber s) ->
                Error(Invalid((Some(addErrDetails errInfoOption "ShortPasswordsMustContainNumbers"))))
            | s when value.Length < 20 && not (containsMixedCase s) ->
                Error(Invalid((Some(addErrDetails errInfoOption "ShortPasswordsMustContainMixedCaseChars"))))
            | s when startsWithPartialStrWhenGtOne s forEmail ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotStartWithSameValueAsEmail"))))
            | s when startsWithPartialStrWhenGtOne s forFirstname ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotStartWithSameValueAsFirstname"))))
            | s when startsWithPartialStrWhenGtOne s forLastname ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotStartWithSameValueAsLastname"))))
            | s when containsPartialStrWhenGtOne s forEmail ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotContainPartOfEmail"))))
            | s when containsPartialStrWhenGtOne s forFirstname ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotContainPartOfFirstname"))))
            | s when containsPartialStrWhenGtOne s forLastname ->
                Error(Invalid((Some(addErrDetails errInfoOption "CannotContainPartOfLastname"))))
            | _ -> Ok(T requiredStr)

        let create pwdStr errInfoOption forEmail forFirstname forLastname =
            (StringRequiredValue.create pwdStr errInfoOption)
            .=> pwdLengthValidation errInfoOption
            .=> pwdStrengthValidation errInfoOption forEmail forFirstname forLastname

        let value (T reqStr) = StringRequiredValue.value reqStr

    module AccountValidation =

        type TokenType =
            | ValidateEmail = 0
            | ResetPassword = 1


        type TokenInfo =
            { Id: Guid
              ExpiresAt: DateTimeOffset
              ForEmail: EmailAddressValue.T
              TokenType: TokenType }

        type Token =
            | ValidToken of TokenInfo
            | ExpiredToken of TokenInfo

        let createToken tokenStr utcNow decrypt =
            //TODO Add comment to explain what happens
            try

                let checkExpiry utcNow tokenInfo =
                    if utcNow >= tokenInfo.ExpiresAt then
                        ExpiredToken tokenInfo
                    else
                        ValidToken tokenInfo

                let parseToken (tokenStr: string) =
                    let fields = tokenStr.Split(";;", StringSplitOptions.RemoveEmptyEntries)
                    if fields.Length <> 4 then
                        Error
                            (InvalidToken
                                ((Some
                                    { ErrInfo.FieldName = Some "token"
                                      ErrInfo.Details = Some("accountValidationToken.parseError.InvalidFieldCount" :: []) })))
                    else
                        let email =
                            EmailAddressValue.create fields.[1]
                                (Some
                                    { ErrInfo.FieldName = Some "token.email"
                                      ErrInfo.Details = Some("accountValidationToken.parseError.invalidEmail" :: []) })

                        email .=> (fun validEmail ->
                                                    Ok
                                                        (let tokenInfo =
                                                            { Id = Guid.Parse fields.[0]
                                                              ForEmail = validEmail
                                                              ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse fields.[2])
                                                              TokenType = Enum.Parse(typeof<TokenType>, fields.[3]) :?> TokenType  }
                                                         checkExpiry utcNow tokenInfo))

                decrypt tokenStr |> parseToken
            with ex ->
                Error
                    (InvalidToken
                        (Some
                            ({ ErrInfo.FieldName = Some "token"
                               Details = Some("accountValidationToken.UnexpectedError" :: [ex.Message])})))

        let createTokenLifetimeDuration tokenType =
            match tokenType with
            |TokenType.ValidateEmail -> TimeSpan.FromDays(3.0)
            |TokenType.ResetPassword -> TimeSpan.FromHours(1.0)
            | _ -> TimeSpan.FromMinutes(1.0)

        let generateAccountTokenString forEmail (tokenIssueDate: DateTimeOffset) encrypt (tokenType:TokenType) =

            let tokenId = Guid.NewGuid()//Arbitrary uniqueIsh string
            let expiresAt = tokenIssueDate.Add(createTokenLifetimeDuration tokenType).ToUnixTimeMilliseconds() //Expiry of token
            let str = sprintf "%s;;%s;;%s;;%d" (tokenId.ToString()) forEmail (expiresAt.ToString()) (tokenType |> int) //embed all relevant data to validate the token in a delimited string
            encrypt (str)//encrypt the string; only the server knows how to encrypt and decrypt this token

    module Organisations =

        type OrganisationTokenType =
            | Invitation = 0
            //| ResetPassword = 1

        type TokenInfo =
            { Id: Guid
              InvitedAt: DateTimeOffset
              ForEmail: EmailAddressValue.T
              OrganisationId: StringRequiredValue.T
              TokenType: OrganisationTokenType }

        type OrganisationToken =
            | ValidToken of TokenInfo
            //| ExpiredToken of TokenInfo


        let createToken tokenStr utcNow decrypt =

            try
                let checkExpiry utcNow tokenInfo =
    //                if utcNow >= tokenInfo.ExpiresAt then
    //                    ExpiredToken tokenInfo
    //                else
                        ValidToken tokenInfo

                let parseToken (tokenStr: string) =
                    let fields = tokenStr.Split(";;", StringSplitOptions.RemoveEmptyEntries)
                    if not (fields.Length = 5) then
                        Error
                            (InvalidToken
                                ((Some
                                    { ErrInfo.FieldName = Some "token"
                                      ErrInfo.Details = Some(["organisationToken.parseError.InvalidFieldCount"]) })))
                    else
                        let email =
                            EmailAddressValue.create fields.[1]
                                (Some
                                    { ErrInfo.FieldName = Some "token.forEmail"
                                      ErrInfo.Details = Some(["organisationToken.parseError.invalidEmail"]) })

                        let orgId =
                            StringRequiredValue.create fields.[2]
                                 (Some
                                    { ErrInfo.FieldName = Some "token.organisationId"
                                      ErrInfo.Details = Some(["organisationToken.parseError.invalidOrganisationId"]) })
                        email .<|>. orgId
                        .=> (fun (validEmail, validOrgId ) ->
                                                    Ok
                                                        (
                                                             let tokenInfo =
                                                                { Id = Guid.Parse fields.[0]
                                                                  ForEmail = validEmail//[1]
                                                                  OrganisationId = validOrgId//[2]
                                                                  InvitedAt = DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse fields.[3])
                                                                  TokenType = Enum.Parse(typeof<OrganisationTokenType>, fields.[4]) :?> OrganisationTokenType}

                                                             checkExpiry utcNow tokenInfo
                                                         )
                            )
                parseToken(decrypt tokenStr)
            with ex ->
                Error
                    (InvalidToken
                        (Some
                            ({ ErrInfo.FieldName = Some "token"
                               Details = Some("organisationToken.UnexpectedError" :: [ex.Message])})))


        let generateOrganisationTokenString forEmail forOrgId (utcNow: DateTimeOffset) encryptFunc (tokenType:OrganisationTokenType) =

            let tokenId = Guid.NewGuid()//Arbitrary uniqueIsh string
            let invitedAt = utcNow.ToUnixTimeMilliseconds() //Expiry of token
            let str = sprintf "%s;;%s;;%s;;%s;;%d" (tokenId.ToString()) forEmail forOrgId (invitedAt.ToString()) (tokenType |> int) //embed all relevant data to validate the token in a delimited string
            encryptFunc (str)//encrypt the string; only the server knows how to encrypt and decrypt this token

    type AuthenticateUserRequest =
        { Email: EmailAddressValue.T
          PasswordValue: PasswordValue.T }

    type RegisterUserRequest =
        { Email: EmailAddressValue.T
          Firstname: StringRequiredValue.T
          Lastname: StringRequiredValue.T
          PasswordValue: PasswordValue.T }

    type ResetPasswordRequest =
        { Token: AccountValidation.Token
          NewPasswordInput: string }

    type AddOrganisationRequest =
        { UserId: EmailAddressValue.T
          OrganisationName: StringRequiredValue.T }

    type UpdateUserRequest =
        { UserId: EmailAddressValue.T
          Firstname: StringRequiredValue.T
          Lastname: StringRequiredValue.T }

    type InviteUserToOrganisationRequest =
        { InviteeEmail: EmailAddressValue.T
          ByUserId: EmailAddressValue.T
          OrganisationId: StringRequiredValue.T }

    type GetAllUsersForOrganisationRequest =
       { RequestedByUserId : EmailAddressValue.T
         OrganisationId: StringRequiredValue.T }

    type DeleteUserFromOrganisationRequest =
       { UserId: EmailAddressValue.T
         RequestedByUserId : EmailAddressValue.T
         OrganisationId: StringRequiredValue.T }
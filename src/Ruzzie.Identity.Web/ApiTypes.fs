namespace Ruzzie.Identity.Web.ApiTypes
open System.ComponentModel.DataAnnotations
open Ruzzie.Extensions.Validation
open Ruzzie.Identity.Web

//TODO: Should this type be in DomainTypes?
type AccountValidationStatus =
    | None = 0
    | Validated = 1
    | Pending = 2

type OrganisationInviteStatus =
    | None = 0
    | Accepted = 1
    | Pending = 2

[<CLIMutable>] //For JSON Serialization
type AuthenticateUserReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<EmailAddressV2(ErrorMessage = "error.invalidEmail")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      email: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(128, MinimumLength = 8, ErrorMessage = "error.invalidPasswordLength")>]
      [<DataType(DataType.Password)>]
      password: string }

[<CLIMutable>] //For JSON Serialization
type AuthenticateUserResp =
    { email: string
      firstname: string
      lastname: string
      createdTimestamp: int64
      systemFeatureToggles: string List
      lastModifiedTimestamp: int64
      JWT: string
      accountValidationStatus: AccountValidationStatus }

[<CLIMutable>] //For JSON Serialization
type RegisterUserResp =
    { email: string
      firstname: string
      lastname: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
      JWT: string
      accountValidationStatus: AccountValidationStatus
      witActivationMail: bool
      activationMailResult: string }

[<CLIMutable>]
type UserOrganisation =
    { id: string
      name: string
      createdByUserId: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
      userRole: string
      userJoinedTimestamp: int64 }

[<CLIMutable>] //For JSON Serialization
type User =
    { id: string
      email: string
      firstname: string
      lastname: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
      accountValidationStatus: AccountValidationStatus //todo: add toggles
      systemFeatureToggles: string List
      organisations: UserOrganisation list }

[<CLIMutable>] //For JSON Serialization
type OrganisationUser =
    { id: string
      email: string
      firstname: string
      lastname: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
      userRole: string
      userJoinedTimestamp: int64
    }

[<CLIMutable>] //For JSON Serialization
type OrganisationInvite =
    { inviteStatus : OrganisationInviteStatus
      inviteeEmail: string
      invitedBy: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
    }

[<CLIMutable>] //For JSON Serialization
type Organisation =
    { id: string
      name: string
      createdByUserId: string
      createdTimestamp: int64
      lastModifiedTimestamp: int64
      users: OrganisationUser list
      pendingInvites: OrganisationInvite list
    }

[<CLIMutable>] //For JSON Serialization
type RegisterUserReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<EmailAddressV2(ErrorMessage = "error.invalidEmail")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      email: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      firstname: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      lastname: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(128, MinimumLength = 8, ErrorMessage = "error.invalidPasswordLength")>]
      [<DataType(DataType.Password)>]
      password: string }

[<CLIMutable>] //For JSON Serialization
type ResetPasswordReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      resetPasswordToken: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(128, MinimumLength = 8, ErrorMessage = "error.invalidPasswordLength")>]
      [<DataType(DataType.Password)>]
      newPassword: string }

[<CLIMutable>] //For JSON Serialization
type AddOrganisationReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, MinimumLength = 3, ErrorMessage = "error.TooLong")>]
      organisationName: string }

[<CLIMutable>] //For JSON Serialization
type UpdateUserReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      firstname: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      lastname: string }

[<CLIMutable>] //For JSON Serialization
type AcceptOrganisationInviteReq =
    { [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      firstname: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      lastname: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(128, MinimumLength = 8, ErrorMessage = "error.invalidPasswordLength")>]
      [<DataType(DataType.Password)>]
      password: string

      [<Required(ErrorMessage = "error.required")>]
      [<StringLength(Constants.Size32K, ErrorMessage = "error.TooLong")>]
      invitationToken: string }
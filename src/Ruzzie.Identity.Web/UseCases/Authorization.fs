namespace Ruzzie.Identity.Web.UseCases

open Ruzzie.Identity.Web

module Authorization =


    type AuthOrgUserIds =
        { userId: EmailAddressValue.T
          organisationId: StringRequiredValue.T }

    type AuthorizedOrganisationUser = private AuthorizedOrganisationUser of AuthOrgUserIds

    let value (AuthorizedOrganisationUser x) = x

    ///Check if a authenticated user is authorized for an organisation. The userId is retrieved from the httpContext.
    /// Returns an Error Unauthorized when the user is not authorized.
    /// the userId and organisationId is returned otherwise in a AuthorizedOrganisationUser type
    let isAuthorizedUserForOrganisationIdStr orgRepository organisationId httpContext =

        let isAuthorizedUserResult = Shared.getAuthorizedUserId httpContext

        let organisationIdValidationResult =
            DomainTypes.StringRequiredValue.create organisationId
                (Some
                    { DomainTypes.ErrInfo.FieldName = "organisationId"
                      DomainTypes.ErrInfo.Details = None })

        isAuthorizedUserResult .<|>. organisationIdValidationResult .=> (fun (userId, orgId) ->
        let isInOrgResult = UseCases.Organisations.userIsInOrganisation orgRepository orgId userId
        match isInOrgResult with
        | Ok isInOrg ->
            if isInOrg = true then
                Ok
                    (AuthorizedOrganisationUser
                        { userId = userId
                          organisationId = orgId })
            else
                Error Unauthorized
        | Error e -> Error e)

﻿namespace Ruzzie.Identity.Web.UseCases

open Ruzzie.Identity.Web
open Ruzzie.Identity.Web.ApiTypes
open Users
open Ruzzie.Identity.Storage.Azure

module Unregister =

    let private deleteUserFromAllOrgs utcNow userRepository orgRepository userId =

        let defaultRes: Result<unit, ErrorKind list> = Ok(()) |> toListOfError

        getUserInfo utcNow userRepository orgRepository userId |> toListOfError
        .=> (fun (userInfo: User) ->
            List.fold
                (fun acc (userOrg: UserOrganisation) ->

                    let delRes =
                        StringRequiredValue.create userOrg.id None |> toListOfError
                        .=> (fun orgId ->
                            Organisations.deleteUserFromOrganisationDb orgRepository orgId userId |> toListOfError
                            //Are there any users left in the organisation..? If not, delete organisation
                            .=> (fun _ ->
                                Organisations.getAllUsersForOrganisationId
                                    orgRepository
                                    (StringRequiredValue.value orgId)
                                |> toListOfError
                                .=> (fun allUsersForOrg ->
                                    if allUsersForOrg.Count = 0 then
                                        //Delete org
                                        Organisations.deleteOrganisation orgRepository (StringRequiredValue.value orgId)
                                        |> toListOfError
                                    else
                                        //Don't delete
                                        Ok(ignore true))))

                    mergeErr delRes acc)
                defaultRes
                userInfo.organisations)

    let unregisterUser
        utcNow
        (userRepository: IUserRepository)
        (orgRepository: IOrganisationRepository)
        requestedByUserId
        userId
        =

        if not (requestedByUserId = userId) then
            Error Unauthorized |> toListOfError
        else

            let deleteUserFromOrgs = (deleteUserFromAllOrgs utcNow userRepository orgRepository userId)
            let deleteUserRes = deleteUser userRepository userId |> toListOfError
            mergeErr deleteUserFromOrgs deleteUserRes

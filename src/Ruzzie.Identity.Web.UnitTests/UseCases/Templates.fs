module Ruzzie.Identity.Web.UnitTests.UseCases.Templates
open Ruzzie.Identity.Web
open Ruzzie.Identity.Web.Email

let userRegistrationActivationMail toEmail activateActionUrl =
    let text =
        sprintf "Welkom bij Testing, \n Om uw account te valideren kunt u deze link %s gebruiken.\n\n Het Testing Team"
            activateActionUrl

    let html =
        sprintf
            "Welkom bij Testing, <br/> Om uw account te valideren kunt u deze link <a href=\"%s\">Activeer</a> gebruiken.<br/><br/> Het Testing Team"
            activateActionUrl
    createSendEmailRequest "Testing <noreply@valid.org>" toEmail None "Uw Testing Account activatie" text html

let userPasswordForgetMail toEmail resetActionUrl =
    let text =
        sprintf
            "Welkom bij Testing, \n Om een nieuw wachtwoord te kiezen kunt u deze link %s gebruiken.\n\n Het Testing Team"
            resetActionUrl

    let html =
        sprintf
            "Welkom bij Testing, <br/> Om een nieuw wachtwoord te kiezen kunt u deze link <a href=\"%s\">Reset wachtwoord</a> gebruiken.<br/><br/> Het Testing Team"
            resetActionUrl
    createSendEmailRequest "Testing <noreply@valid.org>" toEmail None "Uw Testing Account wachtwoord reset" text html

let inviteUserToOrganisationEmail (byUser: ApiTypes.User) (forOrganisation: ApiTypes.UserOrganisation) toEmail
    acceptInvitationUrl =
    let text =
        sprintf
            "Welkom bij Testing, \n De uitnodiging te accepteren kunt u deze link %s gebruiken.\n\n Het Testing Team"
            acceptInvitationUrl

    let html =
        sprintf
            "Welkom bij Testing, <br/> De uitnodiging te accepteren kunt u deze link <a href=\"%s\">Accepteer uitnodiging</a> gebruiken.<br/><br/> Het Testing Team"
            acceptInvitationUrl
    let subject =
        sprintf "U bent uitgenodigd door %s %s van %s voor een Testing Account" byUser.firstname byUser.lastname
            forOrganisation.name
    createSendEmailRequest "Testing <noreply@valid.org>" toEmail None subject text html
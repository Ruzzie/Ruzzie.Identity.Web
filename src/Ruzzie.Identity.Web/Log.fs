namespace Ruzzie.Identity.Web
open Microsoft.Extensions.Logging

module Log = 
    let logErrors (logger:ILogger) (eventId:EventId) errList =
        let errListOfStr = List.map (fun errKind -> toLogString errKind) errList
        let errLogString =  errListOfStr |> List.fold (fun str curr -> str + "\n" + curr) ""
        logger.LogError(eventId, errLogString)
        errLogString

    module Events =
        let SendEmailErrorEvent = EventId(5010, "SendEmailError") //{Id = 5010; Name = "SendEmailError" }


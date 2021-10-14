namespace Ruzzie.Identity.Web

open Microsoft.Extensions.Logging

module Log =

    /// Logs an error to the logger with a given mainErrorMsg and properly prints the given ErrorKind list to strings
    ///   this indents the list of errors and puts each given error in the list on a new line
    ///   like : Something went wrong
    ///          \t Unauthorized message
    ///          \t Unexpected An error occured ...
    ///  etc.
    let logErrorsWithMsg (mainErrorMsg: string) (logger: ILogger) errList =
        let errListOfStr = List.map toLogString errList
        let errLogString = errListOfStr |> List.fold (fun str curr -> "\t" + str + "\n" + curr) mainErrorMsg
        logger.LogError(errLogString)
        errLogString

    let logErrorsWithMsgForEventId (initialMessage: string) (logger: ILogger) (eventId: EventId) errList =
        let errListOfStr = List.map toLogString errList
        let errLogString = errListOfStr |> List.fold (fun str curr -> str + "\n" + curr) initialMessage
        logger.LogError(eventId, errLogString)
        errLogString

    /// Logs a list of ErrorKind errors to the given logger and returns the errorMessage string
    let logErrors (logger: ILogger) (eventId: EventId) errList = logErrorsWithMsgForEventId "" logger eventId errList

    module Events =
        let SendEmailErrorEvent = EventId(5010, "SendEmailError") //{Id = 5010; Name = "SendEmailError" }

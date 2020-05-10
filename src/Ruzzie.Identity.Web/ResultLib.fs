namespace Ruzzie.Identity.Web

module ResultLib =
    let toListOfError r = Result.mapError (fun e -> e :: []) r

    [<CompiledName("AndThen")>]
    let andThen mapFunc result =
        match result with
        | Ok x -> mapFunc x
        | Error e -> Error e

    ///andThen infix operator. Applies a mapping on an Ok result for continuation.
    let (.=>) r m = andThen m r

    let andThenListOfError mapFunc result =
         match result with
            | Ok x -> mapFunc x |> toListOfError
            | Error e -> Error e
    let (.=>*) r m = andThenListOfError m r
    let apply fResult xResult =
        match fResult, xResult with
        | Ok f, Ok x ->
            Ok(f x)
        | Error errs, Ok _ ->
            Error errs
        | Ok _, Error errs ->
            Error errs
        | Error errs1, Error errs2 ->
            // concat both lists of errors
            Error(errs1 @ errs2)
    // Signature: Result<('a -> 'b)> -> Result<'a> -> Result<'b>

    ///Merge errors to list or in list ignore the previous ok
    let mergeErr fResult xResult =
        match fResult, xResult with
        | Ok _, Ok x ->
            Ok(x)
        | Error errs, Ok _ ->
            Error errs
        | Ok _, Error errs ->
            Error errs
        | Error errs1, Error errs2 ->
            // concat both lists of errors
            Error(List.append errs1 errs2 )

    ///Result.map
    let (<!>) = Result.map
    let (<*>) = apply

    let (<.*.>) fResult xResult = apply (toListOfError fResult) (toListOfError xResult)
    let (<*.>) fResult xResult = apply fResult (toListOfError xResult)
    let (<.*>) fResult xResult = apply (toListOfError fResult)  xResult

    ///joins 2 Ok results when both are Ok. Returns the first Error otherwise.
    let joinOk firstResult secondResult =
        match firstResult with
        | Ok f ->
            Result.map (fun s -> (f,s)) secondResult
        | Error err ->
            Error err

    /// Infix operator to join 2 Ok results when both are Ok. Returns the first (left) Error otherwise.
    let (.<|>.) = joinOk

    let andThenJoinOk firstResult andThenFunc =
        match firstResult with
        | Ok f ->
            match andThenFunc f with
            |Ok s -> Ok(f,s)
            |Error err -> Error err
        | Error err ->
            Error err
    let (.=>.) = andThenJoinOk

    let (>=>) f1 f2 arg =
      match f1 arg with
      | Ok data -> f2 data
      | Error e -> Error e

    type Result<'T, 'TError> with
        member inline xs.AndThen mapping = andThen mapping xs

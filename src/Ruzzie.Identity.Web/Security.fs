namespace Ruzzie.Identity.Web

open Ruzzie.Common.Security

module Security =

    let pepper: byte [] =
        [| 4uy
           253uy
           32uy
           29uy
           87uy
           15uy
           186uy
           131uy |]
    

    let hashPassword (passwordHasher: 'a :> IPasswordHasher) userPassword =
        try
            Ok(passwordHasher.HashPassword userPassword)
        with
        | ex -> Error ex

    let verifyHashedPassword (passwordHasher: 'a :> IPasswordHasher) hashedPassword providedPassword =
        try
            Ok(passwordHasher.VerifyHashedPassword(hashedPassword, providedPassword))
        with
        | ex -> Error ex

namespace Ruzzie.Identity.Web

open Ruzzie.Common.Security

module Security =

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

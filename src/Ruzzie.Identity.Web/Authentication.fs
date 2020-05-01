namespace Ruzzie.Identity.Web
open Microsoft.IdentityModel.Tokens
open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Text

module Authentication =

    module JWT =
        [<CLIMutable>]
        type JWTConfig =
            { Secret: string
              ExpirationInMinutes: int
              Issuer: string
              Audience: string }

        let ClaimTypeOnBehalfOf = "on_behalf_of"
        let ClaimTypeCompany = "company"

        let claimsForUser userId userRoles company =
            [Claim(ClaimTypes.Name, userId);
             Claim(JwtRegisteredClaimNames.Sub, userId);
             Claim(ClaimTypeOnBehalfOf, userId);
             Claim(ClaimTypeCompany, company)]  //TODO: Implement this later; set default company when only 1 org
            @ List.map (fun role -> Claim(ClaimTypes.Role, role)) userRoles

        let toClaimsIdentity (claims: Claim list) = ClaimsIdentity(claims)

        let credentialsFor secretKey =
            let securityKey = SymmetricSecurityKey(secretKey)
            SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)

        let generateJWTForUser userId userRoles company (utcNow:DateTimeOffset) jwtConfig =
            let tokenDescriptor =
                SecurityTokenDescriptor
                    (Subject = (toClaimsIdentity <| claimsForUser userId userRoles company),
                     Expires = Nullable(utcNow.UtcDateTime.AddMinutes(jwtConfig.ExpirationInMinutes |> float)),
                     Issuer = jwtConfig.Issuer, Audience = jwtConfig.Audience,
                     SigningCredentials = credentialsFor (Encoding.UTF8.GetBytes(jwtConfig.Secret)))
            JwtSecurityTokenHandler().CreateEncodedJwt(tokenDescriptor)
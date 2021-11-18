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

        [<Literal>]
        let ClaimTypeOnBehalfOf = "on_behalf_of"
        [<Literal>]
        let ClaimTypeOrg = "company"//"org"
        [<Literal>]
        let ClaimTypeOrganisationOwner = "org_owner"
        [<Literal>]
        let ClaimTypeOrganisationMember = "org_member"

        let claimsForUser userId userRoles currentOrganisation organisationsWhereOwner organisationsWhereMember =
            [Claim(ClaimTypes.Name, userId);
             Claim(JwtRegisteredClaimNames.Sub, userId);
             Claim(ClaimTypeOnBehalfOf, userId);
             Claim(ClaimTypeOrg, currentOrganisation)]
            @ List.map (fun orgKey -> Claim(ClaimTypeOrganisationOwner, orgKey)) organisationsWhereOwner
            @ List.map (fun orgKey -> Claim(ClaimTypeOrganisationMember, orgKey)) organisationsWhereMember
            @ List.map (fun role -> Claim(ClaimTypes.Role, role)) userRoles

        let toClaimsIdentity (claims: Claim list) = ClaimsIdentity(claims)

        let credentialsFor secretKey =
            let securityKey = SymmetricSecurityKey(secretKey)
            SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)

        let generateJWTForUser userId userRoles currentOrganisation orgKeysWhereOwner orgKeysWhereMember (utcNow:DateTimeOffset) jwtConfig =
            let tokenDescriptor =
                SecurityTokenDescriptor
                    (Subject = (toClaimsIdentity <| claimsForUser userId userRoles currentOrganisation orgKeysWhereOwner orgKeysWhereMember),
                     Expires = Nullable(utcNow.UtcDateTime.AddMinutes(jwtConfig.ExpirationInMinutes |> float)),
                     Issuer = jwtConfig.Issuer, Audience = jwtConfig.Audience,
                     SigningCredentials = credentialsFor (Encoding.UTF8.GetBytes(jwtConfig.Secret)))
            JwtSecurityTokenHandler().CreateEncodedJwt(tokenDescriptor)
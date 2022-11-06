using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace SanteDB.Rest.OAuth.Token
{
    /// <summary>
    /// Default JWT token mapper
    /// </summary>
    public class JwtClaimMapper : IClaimMapper
    {
        /// <summary>
        /// Mapping information to map to kmnown jwt claim names
        /// </summary>
        private Dictionary<string, string> m_claimTypeMapping = new Dictionary<string, string>()
        {
            { ClaimTypes.Sid, OAuthConstants.ClaimType_Sid },
            { ClaimTypes.Email, OAuthConstants.ClaimType_Email },
            { SanteDBClaimTypes.DefaultRoleClaimType, OAuthConstants.ClaimType_Role },
            { SanteDBClaimTypes.DefaultNameClaimType, OAuthConstants.ClaimType_Name },
            { SanteDBClaimTypes.Realm, OAuthConstants.ClaimType_Realm },
            { SanteDBClaimTypes.Telephone, OAuthConstants.ClaimType_Telephone },
            { SanteDBClaimTypes.Actor, OAuthConstants.ClaimType_Actor },
            { SanteDBClaimTypes.SanteDBScopeClaim, SanteDBClaimTypes.SanteDBScopeClaim },
            { SanteDBClaimTypes.NameIdentifier, OAuthConstants.ClaimType_Subject }
        };

        /// <inheritdoc/>
        public string ExternalTokenFormat => ClaimMapper.ExternalTokenTypeJwt;

        /// <inheritdoc/>
        public IDictionary<string, object> MapToExternalIdentityClaims(IEnumerable<IClaim> internalClaims)
        {
            var mappedClaims = new Dictionary<String, Object>();
            foreach (var claim in internalClaims)
            {
                var claimtype = claim.Type;
                if (this.m_claimTypeMapping.TryGetValue(claimtype, out var newclaimtype))
                {
                    claimtype = newclaimtype;
                    mappedClaims.AddClaim(claimtype, claim.Value);
                }
            }

            return mappedClaims;
        }

        /// <inheritdoc/>
        public IEnumerable<IClaim> MapToInternalIdentityClaims(IDictionary<string, object> externalClaims)
        {
            foreach(var claim in externalClaims)
            {
                var internalClaim = this.m_claimTypeMapping.FirstOrDefault(o => o.Value == claim.Key);
                if(internalClaim.Key == null)
                {
                    continue;
                }

                if (claim.Value is IList list)
                {
                    foreach (var value in list)
                    {
                        yield return this.InterpretExternalClaimValue(internalClaim.Key, value.ToString());
                    }
                }
                else
                {
                    yield return this.InterpretExternalClaimValue(internalClaim.Key, claim.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Interpret any optimizations on the claim value
        /// </summary>
        private IClaim InterpretExternalClaimValue(string type, string value)
        {

            if (type == SanteDBClaimTypes.SanteDBScopeClaim && value.StartsWith("ua."))
            {
                value = $"{PermissionPolicyIdentifiers.UnrestrictedAll}{value.Substring(2)}";
            }

            return new SanteDBClaim(type, value);
        }
    }
}

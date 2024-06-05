/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
            { SanteDBClaimTypes.SanteDBSessionIdClaim, OAuthConstants.ClaimType_Sid },
            { ClaimTypes.Email, OAuthConstants.ClaimType_Email },
            { SanteDBClaimTypes.DefaultRoleClaimType, OAuthConstants.ClaimType_Role },
            { SanteDBClaimTypes.DefaultNameClaimType, OAuthConstants.ClaimType_Name },
            { SanteDBClaimTypes.Realm, OAuthConstants.ClaimType_Realm },
            { SanteDBClaimTypes.Telephone, OAuthConstants.ClaimType_Telephone },
            { SanteDBClaimTypes.Actor, OAuthConstants.ClaimType_Actor },
            { SanteDBClaimTypes.SanteDBScopeClaim, SanteDBClaimTypes.SanteDBScopeClaim },
            { SanteDBClaimTypes.SecurityId, OAuthConstants.ClaimType_Subject },
            { SanteDBClaimTypes.NameIdentifier, OAuthConstants.ClaimType_Subject }
        };

        /// <inheritdoc/>
        public string ExternalTokenFormat => ClaimMapper.ExternalTokenTypeJwt;

        /// <inheritdoc/>
        public string MapToExternalClaimType(string internalClaimType)
        {
            if (String.IsNullOrEmpty(internalClaimType))
            {
                throw new ArgumentNullException(nameof(internalClaimType));
            }
            if (m_claimTypeMapping.TryGetValue(internalClaimType, out var newclaimtype))
            {
                return newclaimtype;
            }

            return internalClaimType;
        }

        /// <inheritdoc/>
        public string MapToInternalClaimType(string externalClaimType) => m_claimTypeMapping.FirstOrDefault(kvp => kvp.Value == externalClaimType).Key ?? externalClaimType;

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
            foreach (var claim in externalClaims)
            {
                // Audience is a one way claim so it doesn't appear in the map
                if (claim.Key.Equals("aud", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new SanteDBClaim(SanteDBClaimTypes.AudienceClaim, claim.Value.ToString());
                }

                var internalClaim = this.m_claimTypeMapping.FirstOrDefault(o => o.Value == claim.Key);
                if (internalClaim.Key == null)
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

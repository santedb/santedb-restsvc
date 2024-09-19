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
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Security.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SanteDB.Rest.OAuth.Token
{
    /// <summary>
    /// Claim mapper for https://profiles.ihe.net/ITI/IUA/index.html#3714221-json-web-token-option
    /// </summary>
    public class IheIuaClaimMapper : IClaimMapper
    {
        private readonly Dictionary<String, String> m_tokenMapping = new Dictionary<string, string>()
        {
            { SanteDBClaimTypes.XspaFacilityClaim, OAuthConstants.IUA_Claim_FacilityId },
            { SanteDBClaimTypes.XspaOrganizationIdClaim, OAuthConstants.IUA_Claim_SubjectOrganizationId },
            { SanteDBClaimTypes.XspaOrganizationNameClaim, OAuthConstants.IUA_Claim_SubjectOrganization },
            { SanteDBClaimTypes.XspaUserNpi, OAuthConstants.IUA_Claim_NationalProviderId },
            { SanteDBClaimTypes.XspaSubjectNameClaim, OAuthConstants.IUA_Claim_SubjectName },
            { SanteDBClaimTypes.XspaPurposeOfUseClaim, OAuthConstants.IUA_Claim_PurposeOfUse },
            { SanteDBClaimTypes.XspaUserRoleClaim, OAuthConstants.IUA_Claim_SubjectRole },
            { SanteDBClaimTypes.CdrEntityId, OAuthConstants.IUA_Claim_PersonId }
        };

        /// <inheritdoc/>
        public string ExternalTokenFormat => ClaimMapper.ExternalTokenTypeJwt;

        /// <inheritdoc/>
        public string MapToExternalClaimType(string internalClaimType)
        {
            if (m_tokenMapping.TryGetValue(internalClaimType, out var newclaimtype))
            {
                return newclaimtype;
            }

            return internalClaimType;
        }

        /// <inheritdoc/>
        public string MapToInternalClaimType(string externalClaimType) => m_tokenMapping.FirstOrDefault(kvp => kvp.Value == externalClaimType).Value ?? externalClaimType;
        /// <inheritdoc/>
        public IDictionary<string, object> MapToExternalIdentityClaims(IEnumerable<IClaim> internalClaims)
        {
            var iuaClaims = new Dictionary<String, Object>();
            foreach (var claim in internalClaims)
            {
                if (this.m_tokenMapping.TryGetValue(claim.Type, out var claimtype))
                {
                    switch (claimtype)
                    {
                        case OAuthConstants.IUA_Claim_SubjectOrganizationId:
                            if (Guid.TryParse(claim.Value, out var claimValue)) // format as urn:uuid:
                            {
                                iuaClaims.AddClaim(claimtype, $"urn:uuid:{claimValue}");
                            }
                            break;
                        case OAuthConstants.IUA_Claim_PurposeOfUse:
                            iuaClaims.AddClaim(claimtype, new { system = "http://santedb.org/conceptset/PurposeOfUse", code = claim.Value });
                            break;
                        case OAuthConstants.IUA_Claim_SubjectRole:
                            var parts = claim.Value.Split('^');
                            iuaClaims.AddClaim(claimtype, new { system = parts[1], code = parts[0] });
                            break;
                        default:
                            iuaClaims.AddClaim(claimtype, claim.Value);
                            break;
                    }
                }
            }

            return new Dictionary<String, Object>()
            {
                {  "extensions", new Dictionary<String, Object>() { { "ihe_iua", iuaClaims }  }
                }
            };
        }

        /// <inheritdoc/>
        public IEnumerable<IClaim> MapToInternalIdentityClaims(IDictionary<string, object> externalClaims)
        {
            if (externalClaims.TryGetValue("extensions", out var extValue) && extValue is ICustomTypeDescriptor extensionClaims)
            {
                var iheProperty = extensionClaims.GetProperties().Find("ihe_iua", true);
                var iuaClaimValue = iheProperty?.GetValue(extValue);
                if (iuaClaimValue is ICustomTypeDescriptor iuaClaims)
                {
                    foreach (object claimPropertyObject in iuaClaims.GetProperties())
                    {
                        if (claimPropertyObject is PropertyDescriptor claimProperty)
                        {
                            var internalClaim = this.m_tokenMapping.FirstOrDefault(o => o.Value == claimProperty.Name);
                            if (internalClaim.Key == null)
                            {
                                continue;
                            }

                            var value = claimProperty.GetValue(iuaClaimValue);
                            if (value is IList list)
                            {
                                foreach (var listValue in list)
                                {
                                    yield return new SanteDBClaim(internalClaim.Key, this.InterpretValue(listValue));
                                }
                            }
                            else
                            {
                                yield return new SanteDBClaim(internalClaim.Key, this.InterpretValue(value));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stringify a complex value from IUA like a FHIR code 
        /// </summary>
        private string InterpretValue(Object value)
        {
            if (value is ICustomTypeDescriptor descriptor)
            {
                var systemProperty = descriptor.GetProperties().Find("system", true);
                var codeProperty = descriptor.GetProperties().Find("code", true);
                var valueProperty = descriptor.GetProperties().Find("value", true);
                if (systemProperty != null && codeProperty != null) // we have a coding or identifier
                {
                    return $"{codeProperty.GetValue(value)}^{systemProperty.GetValue(value)}";
                }
                else if (systemProperty != null && valueProperty != null) // we have a coding or identifier
                {
                    return $"{valueProperty.GetValue(value)}^{systemProperty.GetValue(value)}";
                }
                else
                {
                    throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, value, new { system = "x", code = "y" }));
                }
            }
            else
            {
                return value.ToString().Replace("urn:uuid:", "").Replace("urn:oid:", "");

            }
        }
    }
}

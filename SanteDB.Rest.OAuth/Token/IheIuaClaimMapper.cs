/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.i18n;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace SanteDB.Rest.OAuth.Token
{
    /// <summary>
    /// Claim mapper for https://profiles.ihe.net/ITI/IUA/index.html#3714221-json-web-token-option
    /// </summary>
    public class IheIuaClaimMapper : IClaimMapper
    {

        private readonly Dictionary<String, String> m_tokenMapping = new Dictionary<string, string>()
        {
            { SanteDBClaimTypes.XspaOrganizationIdClaim, OAuthConstants.IUA_Claim_SubjectOrganizationId },
            { SanteDBClaimTypes.XspaOrganizationNameClaim, OAuthConstants.IUA_Claim_SubjectOrganization },
            { SanteDBClaimTypes.XspaUserNpi, OAuthConstants.IUA_Claim_NationalProviderId },
            { SanteDBClaimTypes.XspaSubjectNameClaim, OAuthConstants.IUA_Claim_SubjectName },
            { SanteDBClaimTypes.XspaPurposeOfUseClaim, OAuthConstants.IUA_Claim_PurposeOfUse },
            { SanteDBClaimTypes.XspaUserRoleClaim, OAuthConstants.IUA_Claim_SubjectRole },
            { SanteDBClaimTypes.CdrEntityId, OAuthConstants.IUA_Claim_PersonId }
        };
        private readonly IConceptRepositoryService m_conceptRepositoryService;
        private readonly IRepositoryService<EntityIdentifier> m_eidService;


        /// <summary>
        /// DI CTOR
        /// </summary>
        public IheIuaClaimMapper(
            IConceptRepositoryService conceptRepositoryService = null, 
            IRepositoryService<EntityIdentifier> entityIdentifierService = null)
        {
            this.m_conceptRepositoryService = conceptRepositoryService;
            this.m_eidService = entityIdentifierService;
        }

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
            using (AuthenticationContext.EnterSystemContext())
            {
                var iuaClaims = new Dictionary<String, Object>();
                foreach (var claim in internalClaims)
                {
                    if (this.m_tokenMapping.TryGetValue(claim.Type, out var claimtype))
                    {
                        var parts = claim.Value.Split('^');

                        switch (claimtype)
                        {
                            case OAuthConstants.IUA_Claim_SubjectOrganizationId:
                                if (Guid.TryParse(claim.Value, out var claimValue)) // format as urn:uuid:
                                {
                                    // Attempt lookup 
                                    var eidStd = this.m_eidService?.Find(o => o.SourceEntityKey == claimValue && o.ObsoleteVersionSequenceId == null && o.IdentityDomain.IsUnique).FirstOrDefault();
                                    if (eidStd != null)
                                    {
                                        iuaClaims.AddClaim(claimtype, $"{eidStd.LoadProperty(o => o.IdentityDomain).Url}/{eidStd.Value}");
                                    }
                                    else
                                    {
                                        iuaClaims.AddClaim(claimtype, $"urn:uuid:{claimValue}");
                                    }
                                }
                                break;
                            case OAuthConstants.IUA_Claim_PurposeOfUse:
                                var standardPouCode = this.m_conceptRepositoryService?.GetConceptReferenceTerm(parts[0], "http://terminology.hl7.org/ValueSet/v3-GeneralPurposeOfUse");
                                var pouCodes = new List<Dictionary<String, Object>>();
                                if (standardPouCode != null)
                                {
                                    pouCodes.Add(new Dictionary<String, object> {
                                    { "system", "http://terminology.hl7.org/ValueSet/v3-GeneralPurposeOfUse" },
                                    { "code", standardPouCode.Mnemonic },
                                    { "display", standardPouCode.GetDisplayName("en") }
                                });
                                }

                                // On WWW there may be no concept mapper - so this is necessary to preserve the original key/value pair
                                pouCodes.Add(new Dictionary<string, object>()
                            {
                                { "system", parts.Length > 1 ? parts[1] : "http://santedb.org/concept" },
                                {  "code", parts[0] }
                            });

                                iuaClaims.AddClaim(claimtype, pouCodes);
                                break;
                            case OAuthConstants.IUA_Claim_SubjectRole:
                                iuaClaims.AddClaim(claimtype, new Dictionary<String, Object> {
                                { "system", parts[1] },
                                { "code", parts[0] }
                            });
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
        }

        /// <inheritdoc/>
        public IEnumerable<IClaim> MapToInternalIdentityClaims(IDictionary<string, object> externalClaims)
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                if (externalClaims.TryGetValue("extensions", out var extValue) && extValue is JsonElement extensionElement)
                {
                    var iheProperty = extensionElement.EnumerateObject().FirstOrDefault(o => o.Name == "ihe_iua");
                    if (iheProperty.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var claimPropertyObject in iheProperty.Value.EnumerateObject().OfType<JsonProperty>())
                        {
                            var internalClaim = this.m_tokenMapping.FirstOrDefault(o => o.Value == claimPropertyObject.Name);
                            if (internalClaim.Key == null)
                            {
                                continue;
                            }

                            var value = claimPropertyObject.Value;
                            if (value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var listValue in value.EnumerateArray())
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
        private string InterpretValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.ToString().Replace("urn:uuid:", "").Replace("urn:oid:", "");
                case JsonValueKind.Number:
                    return value.ToString();
                case JsonValueKind.Object:
                    var systemValue = value.EnumerateObject().FirstOrDefault(o => o.Name == "system");
                    var codeValue = value.EnumerateObject().FirstOrDefault(o => o.Name == "code");
                    var valueValue = value.EnumerateObject().FirstOrDefault(o => o.Name == "value");
                    if (systemValue.Name != null && codeValue.Name != null) // we have a coding or identifier
                    {
                        // Attempt lookup 
                        if(systemValue.Value.GetString() == "http://santedb.org/concept")
                        {
                            return codeValue.Value.GetString();
                        }
                        else
                        {
                            var lookup = this.m_conceptRepositoryService?.GetConceptByReferenceTerm(codeValue.Value.GetString(), systemValue.Value.GetString());
                            if(lookup == null)
                            {
                                return $"{codeValue.Value.GetString()}^{systemValue.Value.GetString()}";
                            }
                            return lookup.Mnemonic;
                        }
                    }
                    else if (systemValue.Name != null && valueValue.Name != null) // we have a coding or identifier
                    {
                        return $"{valueValue.Value.GetString()}^{systemValue.Value.GetString()}";
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, value, new { system = "x", code = "y" }));
                    }
                default:
                    throw new InvalidOperationException();
            }

        }
    }
}

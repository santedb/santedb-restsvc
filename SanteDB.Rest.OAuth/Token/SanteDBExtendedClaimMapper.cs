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
using SanteDB.Core.Security.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace SanteDB.Rest.OAuth.Token
{
    /// <summary>
    /// Claims mapper that maps SanteDB extended claims onto JWT
    /// </summary>
    public class SanteDBExtendedClaimMapper : IClaimMapper
    {
#pragma warning disable CS1591
        public const string TemporarySessionJwtClaim = "temporary";
        public const string LanguageJwtClaim = "lang";
        public const string MustChangePasswordJwtClaim = "pwd_reset";
        public const string X509CertificateSubjectJwtClaim = "x509sub";
        public const string ApplicationSubjectJwtClaim = "appid";
        public const string ApplicationNameJwtClaim = "appname";
        public const string DeviceSubjectJwtClaim = "devid";
        public const string DeviceNameJwtClaim = "devname";
        public const string UserSubjectJwtClaim = "usrid";
        public const string OverrideJwtClaim = "isOverride";

#pragma warning restore CS1591

        /// <summary>
        /// Mapping information to map to kmnown jwt claim names
        /// </summary>
        private Dictionary<string, string> m_claimTypeMapping = new Dictionary<string, string>()
        {
            { SanteDBClaimTypes.TemporarySession, TemporarySessionJwtClaim },
            { SanteDBClaimTypes.SanteDBOverrideClaim, OverrideJwtClaim },
            { SanteDBClaimTypes.Language, LanguageJwtClaim },
            { SanteDBClaimTypes.ForceResetPassword, MustChangePasswordJwtClaim },
            { SanteDBClaimTypes.AuthenticationCertificateSubject, X509CertificateSubjectJwtClaim },
            { SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, ApplicationSubjectJwtClaim  },
            { SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, DeviceSubjectJwtClaim  },
            { SanteDBClaimTypes.SanteDBUserIdentifierClaim, UserSubjectJwtClaim  },
            { SanteDBClaimTypes.SanteDBApplicationNameClaim, ApplicationNameJwtClaim },
            { SanteDBClaimTypes.SanteDBDeviceNameClaim, DeviceNameJwtClaim }
        };

        /// <inheritdoc/>
        public string ExternalTokenFormat => ClaimMapper.ExternalTokenTypeJwt;

        /// <inheritdoc/>
        public string MapToExternalClaimType(string internalClaimType)
        {
            if (m_claimTypeMapping.TryGetValue(internalClaimType, out var newclaimtype))
            {
                return newclaimtype;
            }

            return internalClaimType;
        }

        /// <inheritdoc/>
        public string MapToInternalClaimType(string externalClaimType) => m_claimTypeMapping.FirstOrDefault(kvp => kvp.Value == externalClaimType).Value ?? externalClaimType;

        /// <inheritdoc/>
        public IDictionary<string, object> MapToExternalIdentityClaims(IEnumerable<IClaim> internalClaims)
        {
            var santedbClaims = new Dictionary<String, Object>();
            foreach (var claim in internalClaims)
            {
                if (this.m_claimTypeMapping.TryGetValue(claim.Type, out var jwtType))
                {
                    santedbClaims.AddClaim(jwtType, claim.Value);
                }
            }

            return new Dictionary<String, Object>()
            {
                {  "extensions", new Dictionary<String, Object>() { { "santedb", santedbClaims }  }
                }
            };
        }

        /// <inheritdoc/>
        public IEnumerable<IClaim> MapToInternalIdentityClaims(IDictionary<string, object> externalClaims)
        {
            if (externalClaims.TryGetValue("extensions", out var extValue) && extValue is JsonElement extensionElement)
            {
                var iheProperty = extensionElement.EnumerateObject().FirstOrDefault(o => o.Name == "santedb");
                if (iheProperty.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var claimPropertyObject in iheProperty.Value.EnumerateObject().OfType<JsonProperty>())
                    {
                        var internalClaim = this.m_claimTypeMapping.FirstOrDefault(o => o.Value == claimPropertyObject.Name);
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
                        return $"{codeValue.Value.GetString()}^{systemValue.Value.GetString()}";
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

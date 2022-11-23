using SanteDB.Core.Security.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.OAuth.Token
{
    /// <summary>
    /// Claims mapper that maps SanteDB extended claims onto JWT
    /// </summary>
    public class SanteDBExtendedClaimMapper : IClaimMapper
    {

        public const string TemporarySessionJwtClaim = "temporary";
        public const string LanguageJwtClaim = "lang";
        public const string MustChangePasswordJwtClaim = "pwd_reset";
        public const string X509CertificateSubjectJwtClaim = "x509sub";
        public const string ApplicationSubjectJwtClaim = "appid";
        public const string DeviceSubjectJwtClaim = "devid";
        public const string UserSubjectJwtClaim = "usrid";
        
        /// <summary>
        /// Mapping information to map to kmnown jwt claim names
        /// </summary>
        private Dictionary<string, string> m_claimTypeMapping = new Dictionary<string, string>()
        {
            { SanteDBClaimTypes.TemporarySession, TemporarySessionJwtClaim },
            { SanteDBClaimTypes.Language, LanguageJwtClaim },
            { SanteDBClaimTypes.ForceResetPassword, MustChangePasswordJwtClaim },
            { SanteDBClaimTypes.AuthenticationCertificateSubject, X509CertificateSubjectJwtClaim },
            { SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, ApplicationSubjectJwtClaim  },
            { SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, DeviceSubjectJwtClaim  },
            { SanteDBClaimTypes.SanteDBUserIdentifierClaim, UserSubjectJwtClaim  }
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
            var retVal = new Dictionary<String, Object>();
            foreach(var itm in internalClaims)
            {
                if(this.m_claimTypeMapping.TryGetValue(itm.Type, out var jwtType))
                {
                    retVal.AddClaim(jwtType, itm.Value);
                }
            }
            return new Dictionary<String, Object>()
            {
                { "extensions", new Dictionary<String, Object>()
                {
                    {"santedb", retVal }
                } }
            };
        }

        /// <inheritdoc/>
        public IEnumerable<IClaim> MapToInternalIdentityClaims(IDictionary<string, object> externalClaims)
        {
            if (externalClaims.TryGetValue("extensions", out var extValue) && extValue is ICustomTypeDescriptor extensionClaims)
            {
                var santeDbProperty = extensionClaims.GetProperties().Find("santedb", true);
                var santeDbPropertyValue = santeDbProperty?.GetValue(extValue);
                if (santeDbPropertyValue is ICustomTypeDescriptor santeDbClaims)
                {
                    foreach (object claimPropertyObject in santeDbClaims.GetProperties())
                    {
                        if (claimPropertyObject is PropertyDescriptor claimProperty)
                        {
                            var internalClaim = this.m_claimTypeMapping.FirstOrDefault(o => o.Value == claimProperty.Name);
                            if (internalClaim.Key == null)
                            {
                                continue;
                            }

                            var value = claimProperty.GetValue(santeDbPropertyValue);
                            if (value is IList list)
                            {
                                foreach (var listValue in list)
                                {
                                    yield return new SanteDBClaim(internalClaim.Key, listValue.ToString());
                                }
                            }
                            else
                            {
                                yield return new SanteDBClaim(internalClaim.Key, value.ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}

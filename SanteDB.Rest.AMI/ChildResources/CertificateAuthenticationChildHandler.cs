/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// A child which adds the <c>certificate</c> resource to users, devices and application
    /// </summary>
    public class CertificateAuthenticationChildHandler : IApiChildResourceHandler
    {
        private readonly ICertificateIdentityProvider m_certificateIdentityProvider;
        private readonly IDeviceIdentityProviderService m_deviceIdentityProvider;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CertificateAuthenticationChildHandler));
        private readonly IApplicationIdentityProviderService m_applicationIdentityProvider;
        private readonly IIdentityProviderService m_identityProvider;
        private readonly IPlatformSecurityProvider m_platformSecurityProvider;

        /// <summary>
        /// DI constructor
        /// </summary>
        public CertificateAuthenticationChildHandler(ICertificateIdentityProvider certificateIdentityProvider,
            IDeviceIdentityProviderService deviceIdentityProviderService,
            IApplicationIdentityProviderService applicationIdentityProviderService,
            IPlatformSecurityProvider platformSecurityProvider,
            IIdentityProviderService identityProviderService)
        {
            this.m_certificateIdentityProvider = certificateIdentityProvider;
            this.m_deviceIdentityProvider = deviceIdentityProviderService;
            this.m_applicationIdentityProvider = applicationIdentityProviderService;
            this.m_identityProvider = identityProviderService;
            this.m_platformSecurityProvider = platformSecurityProvider;
        }

        /// <inheritdoc/>
        public string Name => "auth_cert";

        /// <inheritdoc/>
        public Type PropertyType => typeof(X509Certificate2Info);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create |
            ResourceCapabilityType.Delete |
            ResourceCapabilityType.Update |
            ResourceCapabilityType.Search |
            ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(SecurityUser),
            typeof(SecurityDevice),
            typeof(SecurityApplication)
        };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            // Add a security object
            if (scopingKey is Guid sid)
            {
                IIdentity identityToMap = null;
                switch (scopingType.Name)
                {
                    case nameof(SecurityUser):
                        identityToMap = this.m_identityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityApplication):
                        identityToMap = this.m_applicationIdentityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityDevice):
                        identityToMap = this.m_deviceIdentityProvider.GetIdentity(sid);
                        break;
                    default:
                        m_tracer.TraceWarning("Adding a certificate to object type {0} is not permitted.", scopingType.Name);
                        break;
                }
                if (identityToMap == null)
                {
                    throw new KeyNotFoundException(sid.ToString());
                }

                switch (item)
                {
                    case X509Certificate2Info certInfo:
                        if (certInfo.PublicKey != null)
                        {
                            this.m_certificateIdentityProvider.AddIdentityMap(identityToMap, new X509Certificate2(certInfo.PublicKey), AuthenticationContext.Current.Principal);
                        }
                        else
                        {
                            this.m_certificateIdentityProvider.AddIdentityMap(identityToMap, this.FindCertificateByThumbprint(certInfo.Thumbprint), AuthenticationContext.Current.Principal);
                        }
                        return certInfo;
                    case IEnumerable<MultiPartFormData> multiPartData:
                        var source = multiPartData.FirstOrDefault(o => o.Name == "certificate");
                        if (source?.IsFile == true)
                        {
                            var cert = new X509Certificate2(source.Data);
                            this.m_certificateIdentityProvider.AddIdentityMap(identityToMap, cert, AuthenticationContext.Current.Principal);
                            return new X509Certificate2Info(cert);
                        }
                        else
                        {
                            throw new ArgumentException("Expected certificate file", nameof(item));
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }

        /// <summary>
        /// Find the certificate by thumbprint
        /// </summary>
        private X509Certificate2 FindCertificateByThumbprint(string thumbprint)
        {
            // Find the certificate
            if (!this.m_platformSecurityProvider.TryGetCertificate(X509FindType.FindByThumbprint, thumbprint, out var cert) &&
                !this.m_platformSecurityProvider.TryGetCertificate(X509FindType.FindByThumbprint, thumbprint, StoreName.TrustedPeople, out cert))
            {
                throw new InvalidOperationException(ErrorMessages.CERTIFICATE_NOT_FOUND);
            }
            return cert;
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var search = new NameValueCollection();
            search.Add("thumbprint", key.ToString());
            var certificate = this.Query(scopingType, scopingKey, search).FirstOrDefault() as X509Certificate2Info ?? throw new KeyNotFoundException();
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-pem-file";
            RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename=\"AUTH-{scopingKey}.cer\"");
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(new X509Certificate2(certificate.PublicKey).GetAsPemString()));
            return ms;

        }


        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            // Add a security object
            if (scopingKey is Guid sid)
            {

                IIdentity identityToMap = null;
                switch (scopingType.Name)
                {
                    case nameof(SecurityUser):
                        identityToMap = this.m_identityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityApplication):
                        identityToMap = this.m_applicationIdentityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityDevice):
                        identityToMap = this.m_deviceIdentityProvider.GetIdentity(sid);
                        break;
                }
                if (identityToMap == null)
                {
                    throw new KeyNotFoundException(sid.ToString());
                }

                var searchFilter = QueryExpressionParser.BuildLinqExpression<X509Certificate2Info>(filter, null).Compile();
                // Find the certificate
                return new MemoryQueryResultSet<X509Certificate2Info>(this.m_certificateIdentityProvider.GetIdentityCertificates(identityToMap).Select(o => new X509Certificate2Info(o)).Where(searchFilter));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            // Add a security object
            if (scopingKey is Guid sid)
            {

                IIdentity identityToMap = null;
                switch (scopingType.Name)
                {
                    case nameof(SecurityUser):
                        identityToMap = this.m_identityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityApplication):
                        identityToMap = this.m_applicationIdentityProvider.GetIdentity(sid);
                        break;
                    case nameof(SecurityDevice):
                        identityToMap = this.m_deviceIdentityProvider.GetIdentity(sid);
                        break;
                }
                if (identityToMap == null)
                {
                    throw new KeyNotFoundException(sid.ToString());
                }

                // Remove the certificate key which should match the thumbprint
                var cert = this.m_certificateIdentityProvider.GetIdentityCertificates(identityToMap).FirstOrDefault(o => o.Thumbprint.Equals(key.ToString(), StringComparison.InvariantCultureIgnoreCase));
                this.m_certificateIdentityProvider.RemoveIdentityMap(identityToMap, cert, AuthenticationContext.Current.Principal);
                return new X509Certificate2Info(cert);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }
    }
}

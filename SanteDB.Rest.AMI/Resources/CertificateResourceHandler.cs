﻿/*
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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Certificate resource handler allows for the maintenance of certificates in the local machine or user store
    /// </summary>
    public class CertificateResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// The name of the store name parameter
        /// </summary>
        public const string CertificateStoreParameterName = "storeName";
        /// <summary>
        /// The name of hte private key parameter
        /// </summary>
        public const string PrivateKeyParameterName = "hasPrivateKey";
        /// <summary>
        /// The name of hte password header
        /// </summary>
        public const string PrivateKeyPasswordHeaderName = "X-Pfx-KeyAuthorization";

        private readonly IPolicyEnforcementService m_pepService;
        private readonly IPlatformSecurityProvider m_platformSecurityService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public CertificateResourceHandler(IPolicyEnforcementService policyEnforcementService, IPlatformSecurityProvider platformSecurity)
        {
            this.m_pepService = policyEnforcementService;
            this.m_platformSecurityService = platformSecurity;
        }

        /// <inheritdoc/>
        public string ResourceName => "Certificate";

        /// <inheritdoc/>
        public Type Type => typeof(X509Certificate2Info);

        /// <inheritdoc/>
        public Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        private bool TryParseRestParameters(out StoreName storeName, out string password, out bool privateKey)
        {
            var queryString = RestOperationContext.Current.IncomingRequest.QueryString;
            var retVal = true;

            if (!string.IsNullOrEmpty(queryString[CertificateStoreParameterName]))
            {
                retVal &= Enum.TryParse<StoreName>(queryString[CertificateStoreParameterName], out storeName);
            }
            else
            {
                storeName = StoreName.My;
            }

            if (!String.IsNullOrEmpty(queryString[PrivateKeyParameterName]))
            {
                retVal &= Boolean.TryParse(queryString[PrivateKeyParameterName], out privateKey);
            }
            else
            {
                privateKey = false;
            }

            password = RestOperationContext.Current.IncomingRequest.Headers[PrivateKeyPasswordHeaderName];
            return retVal;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate)]
        public object Create(object data, bool updateIfExists)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well
            X509Certificate2 certificate = null;
            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeName, out var password, out _) ||
                storeName != StoreName.My)
            {
                throw new ArgumentOutOfRangeException();
            }

            switch (data)
            {
                case Stream stream:
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        if (!String.IsNullOrEmpty(password))
                        {
                            certificate = new X509Certificate2(ms.ToArray(), password, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet);
                        }
                        else
                        {
                            certificate = new X509Certificate2(ms.ToArray());
                        }
                    }
                    break;
                case String str:
                    // PEM encoding?
                    certificate = new X509Certificate2(Encoding.UTF8.GetBytes(str));
                    break;
                case X509Certificate2Info certInfo:
                    if (certInfo.PublicKey == null)
                    {
                        throw new ArgumentException();
                    }

                    certificate = new X509Certificate2(certInfo.PublicKey);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data));
            }

            using (var store = new X509Store(storeName, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-x509-cert";
            return this.GetSerializedPublicCert(certificate);
        }

        /// <summary>
        /// Get serialized public certificate in base64 format
        /// </summary>
        private Stream GetSerializedPublicCert(X509Certificate2 certificate)
        {
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-pem-file";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(certificate.GetAsPemString()));
            return ms;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate)]
        public object Delete(object key)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well

            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeName, out var password, out _))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!this.m_platformSecurityService.TryGetCertificate(X509FindType.FindByThumbprint, key.ToString(), storeName, out var certificate))
            {
                throw new KeyNotFoundException(key.ToString());
            }
            _ = this.m_platformSecurityService.TryUninstallCertificate(certificate, storeName);
            return this.GetSerializedPublicCert(certificate);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate)]
        public object Get(object id, object versionId)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well

            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeName, out var password, out bool privateKey))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!this.m_platformSecurityService.TryGetCertificate(X509FindType.FindByThumbprint, id.ToString(), storeName, out var certificate))
            {
                throw new KeyNotFoundException(id.ToString());
            }

            if (!String.IsNullOrEmpty(password) && privateKey && certificate.HasPrivateKey)
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-pkcs12";
                return new MemoryStream(certificate.Export(X509ContentType.Pfx, password));
            }
            else
            {
                return this.GetSerializedPublicCert(certificate);
            }

        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeName, out var password, out bool privateKey))
            {
                throw new ArgumentOutOfRangeException();
            }

            if(privateKey)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well
            }

            IEnumerable<X509Certificate2> certificates = null;
            if (!String.IsNullOrEmpty(queryParameters["subject"]))
            {
                certificates = this.m_platformSecurityService.FindAllCertificates(X509FindType.FindBySubjectDistinguishedName, queryParameters["subject"], validOnly: false);
            }
            else if (!String.IsNullOrEmpty(queryParameters["thumbprint"]))
            {
                certificates = this.m_platformSecurityService.FindAllCertificates(X509FindType.FindByThumbprint, queryParameters["thumbprint"], validOnly: false);
            }
            else
            {
                certificates = this.m_platformSecurityService.FindAllCertificates(X509FindType.FindByTimeValid, DateTime.Now.AddMonths(1), validOnly: false);
            }

            var results = certificates.Select(o => new X509Certificate2Info(o));
            if (privateKey)
            {
                return new MemoryQueryResultSet(results.Where(o => o.HasPrivateKey));
            }
            else
            {
                return new MemoryQueryResultSet(results);

            }
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}

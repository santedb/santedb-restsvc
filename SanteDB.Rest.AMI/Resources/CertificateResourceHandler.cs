using RestSrvr;
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
        public const string CertificateStoreParameterName = "storeName";
        public const string CertificateLocationParameterName = "storeLocation";
        public const string PrivateKeyParameterName = "hasPrivateKey";
        public const string PrivateKeyPasswordHeaderName = "X-Pfx-KeyAuthorization";
        private readonly IPolicyEnforcementService m_pepService;

        public CertificateResourceHandler(IPolicyEnforcementService policyEnforcementService)
        {
            this.m_pepService = policyEnforcementService;
        }
        /// <inheritdoc/>
        public string ResourceName => "Certificate";

        /// <inheritdoc/>
        public Type Type => typeof(X509Certificate2Info);

        /// <inheritdoc/>
        public Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        private bool TryParseRestParameters(out StoreLocation storeLocation, out StoreName storeName, out string password, out bool privateKey)
        {
            var queryString = RestOperationContext.Current.IncomingRequest.QueryString;
            var retVal = true;
            if (!String.IsNullOrEmpty(queryString[CertificateLocationParameterName]))
            {
                retVal &= Enum.TryParse<StoreLocation>(queryString[CertificateLocationParameterName], out storeLocation);
            }
            else
            {
                storeLocation = StoreLocation.CurrentUser;
            }

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
            if (!this.TryParseRestParameters(out var storeLocation, out var storeName, out var password, out _))
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(data));
            }

            using (var store = new X509Store(storeName, storeLocation))
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
            var ms = new MemoryStream();
            using (var tw = new StreamWriter(ms))
            {
                tw.WriteLine("-----BEGIN CERTIFICATE-----");
                tw.WriteLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
                tw.WriteLine("-----END CERTIFICATE-----");
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate)]
        public object Delete(object key)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well

            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeLocation, out var storeName, out var password, out _))
            {
                throw new ArgumentOutOfRangeException();
            }

            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, key.ToString(), true);
                if (certificate.Count != 1)
                {
                    throw new KeyNotFoundException(key.ToString());
                }
                else
                {
                    store.Remove(certificate[0]);

                    return this.GetSerializedPublicCert(certificate[0]);
                }
            }

        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate)]
        public object Get(object id, object versionId)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedCertificate); // require direct calls to pass validation as well

            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeLocation, out var storeName, out var password, out bool privateKey))
            {
                throw new ArgumentOutOfRangeException();
            }

            var certificate = X509CertificateUtils.FindCertificate(X509FindType.FindByThumbprint, storeLocation, storeName, id.ToString());
            if (!String.IsNullOrEmpty(password) && privateKey)
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
        [Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction); // require direct calls to pass validation as well
            // Attempt to get the parameters
            if (!this.TryParseRestParameters(out var storeLocation, out var storeName, out var password, out bool privateKey))
            {
                throw new ArgumentOutOfRangeException();
            }

            using(var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certificates = null;
                if (!String.IsNullOrEmpty(queryParameters["subject"]))
                {
                    certificates = store.Certificates.Find(X509FindType.FindBySubjectName, queryParameters["subject"], true);
                }
                else if (!String.IsNullOrEmpty(queryParameters["thumbprint"]))
                {
                    certificates = store.Certificates.Find(X509FindType.FindByThumbprint, queryParameters["thumbprint"], true);
                }
                else
                {
                    certificates = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now.AddMonths(1), true);
                }

                var results = certificates.OfType<X509Certificate>().Select(o => new X509Certificate2Info(o));
                if(privateKey)
                {
                    return new MemoryQueryResultSet(results.Where(o=>o.HasPrivateKey));
                }
                else
                {
                    return new MemoryQueryResultSet(results);

                }
            }
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}

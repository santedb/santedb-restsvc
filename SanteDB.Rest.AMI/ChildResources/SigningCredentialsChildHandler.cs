using RestSrvr;
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
using SanteDB.Core;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Represents a child resource handler which is used for mapping signing credentials
    /// </summary>
    public class SigningCredentialsChildHandler : IApiChildResourceHandler
    {
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(SigningCredentialsChildHandler));
        private readonly IDataSigningCertificateManagerService m_dataSigningCertificateManager;
        private readonly IPolicyEnforcementService m_pepService;
        private readonly IApplicationIdentityProviderService m_applicationIdentityProvider;
        private readonly IDeviceIdentityProviderService m_deviceIdentityProvider;
        private readonly IPlatformSecurityProvider m_platformSecurityProvider;
        private readonly IIdentityProviderService m_identityProvider;

        /// <summary>
        /// DI ctor
        /// </summary>
        public SigningCredentialsChildHandler(IDataSigningCertificateManagerService dataSigningCertificateManagerService,
            IApplicationIdentityProviderService applicationIdentityProvider,
            IDeviceIdentityProviderService deviceIdentityProviderService,
            IPlatformSecurityProvider platformSecurityProvider,
            IIdentityProviderService identityProviderService)
        {
            this.m_dataSigningCertificateManager = dataSigningCertificateManagerService;
            this.m_applicationIdentityProvider = applicationIdentityProvider;
            this.m_deviceIdentityProvider = deviceIdentityProviderService;
            this.m_platformSecurityProvider = platformSecurityProvider;
            this.m_identityProvider = identityProviderService;
        }

        /// <inheritdoc/>
        public string Name => "dsig_cert";

        /// <inheritdoc/>
        public Type PropertyType => typeof(X509Certificate2Info);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | 
            ResourceCapabilityType.Update | 
            ResourceCapabilityType.Delete | 
            ResourceCapabilityType.Search |
            ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(SecurityDevice),
            typeof(SecurityApplication),
            typeof(SecurityUser)
        };


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
                            this.m_dataSigningCertificateManager.AddSigningCertificate(identityToMap, new X509Certificate2(certInfo.PublicKey), AuthenticationContext.Current.Principal);
                        }
                        else
                        {
                            this.m_dataSigningCertificateManager.AddSigningCertificate(identityToMap, this.FindCertificateByThumbprint(certInfo.Thumbprint), AuthenticationContext.Current.Principal);
                        }
                        return certInfo;
                    case IEnumerable<MultiPartFormData> multiPartData:
                        var source = multiPartData.FirstOrDefault(o => o.Name == "certificate");
                        if (source?.IsFile == true)
                        {
                            var cert = new X509Certificate2(source.Data);
                            this.m_dataSigningCertificateManager.AddSigningCertificate(identityToMap, cert, AuthenticationContext.Current.Principal);
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

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var search = new NameValueCollection();
            search.Add("thumbprint", key.ToString());
            var certificate = this.Query(scopingType, scopingKey, search).FirstOrDefault() as X509Certificate2Info ?? throw new KeyNotFoundException();
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-pem-file";
            RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename=\"SIG-{scopingKey}.cer\"");
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

                // Find the certificate
                var searchFilter = QueryExpressionParser.BuildLinqExpression<X509Certificate2Info>(filter, null).Compile();
                // Find the certificate
                return new MemoryQueryResultSet<X509Certificate2Info>(this.m_dataSigningCertificateManager.GetSigningCertificates(identityToMap).Select(o => new X509Certificate2Info(o)).Where(searchFilter));
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
                var cert = this.m_dataSigningCertificateManager.GetSigningCertificates(identityToMap).FirstOrDefault(o => o.Thumbprint.Equals(key.ToString(), StringComparison.InvariantCultureIgnoreCase));
                this.m_dataSigningCertificateManager.RemoveSigningCertificate(identityToMap, cert, AuthenticationContext.Current.Principal);
                return new X509Certificate2Info(cert); 
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }
    }
}

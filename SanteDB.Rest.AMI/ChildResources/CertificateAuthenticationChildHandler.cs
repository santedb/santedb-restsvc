using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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

        /// <summary>
        /// DI constructor
        /// </summary>
        public CertificateAuthenticationChildHandler(ICertificateIdentityProvider certificateIdentityProvider,
            IDeviceIdentityProviderService deviceIdentityProviderService,
            IApplicationIdentityProviderService applicationIdentityProviderService,
            IIdentityProviderService identityProviderService)
        {
            this.m_certificateIdentityProvider = certificateIdentityProvider;
            this.m_deviceIdentityProvider = deviceIdentityProviderService;
            this.m_applicationIdentityProvider = applicationIdentityProviderService;
            this.m_identityProvider = identityProviderService;
        }

        /// <inheritdoc/>
        public string Name => "certificate";

        /// <inheritdoc/>
        public Type PropertyType => typeof(X509Certificate2Info);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Delete | ResourceCapabilityType.Update | ResourceCapabilityType.Search;

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
            if(scopingKey is Guid sid && item is X509Certificate2Info certInfo)
            {

                IIdentity identityToMap = null;
                switch(scopingType.Name)
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
                if(identityToMap == null)
                {
                    throw new KeyNotFoundException(sid.ToString());
                }

                this.m_certificateIdentityProvider.AddIdentityMap(identityToMap, this.FindCertificateByThumbprint(certInfo.Thumbprint), AuthenticationContext.Current.Principal);
                return certInfo;
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
            var cert = X509CertificateUtils.FindCertificate(System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser, System.Security.Cryptography.X509Certificates.StoreName.My, thumbprint) ??
                X509CertificateUtils.FindCertificate(System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser, System.Security.Cryptography.X509Certificates.StoreName.TrustedPeople, thumbprint);
            if (cert == null)
            {
                throw new InvalidOperationException(ErrorMessages.CERTIFICATE_NOT_FOUND);
            }
            return cert;
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
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
                return new MemoryQueryResultSet<X509Certificate2Info>(this.m_certificateIdentityProvider.GetIdentityCertificates(identityToMap).Select(o=>new X509Certificate2Info(o)));
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
                var cert = this.FindCertificateByThumbprint(key.ToString());
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

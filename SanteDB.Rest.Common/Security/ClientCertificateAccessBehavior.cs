﻿using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Security
{
    /// <summary>
    /// CORS settings
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    [XmlRoot(nameof(ClientCertificateAccessConfiguration), Namespace = "http://santedb.org/configuration")]
    [XmlType(nameof(ClientCertificateAccessConfiguration), Namespace = "http://santedb.org/configuration")]
    public class ClientCertificateAccessConfiguration
    {
        /// <summary>
        /// Resources
        /// </summary>
        public ClientCertificateAccessConfiguration()
        {
            this.RevokationCheckMode = X509RevocationMode.NoCheck;
            this.TrustedIssuers = new List<X509ConfigurationElement>();
        }

        /// <summary>
        /// The revoke check mode
        /// </summary>
        [XmlAttribute("revokeCheck")]
        public X509RevocationMode RevokationCheckMode { get; set; }

        /// <summary>
        /// Gets the resource settings
        /// </summary>
        [XmlArray("trustedIssuers"), XmlArrayItem("add")]
        public List<X509ConfigurationElement> TrustedIssuers { get; private set; }

        /// <summary>
        /// When set to true allows an upstream HTTP reverse proxy to append a X-SSL-ClientCert header with a 
        /// PFX or PEM encoded copy of the client certificate which can then be validated by this server
        /// </summary>
        [XmlElement("allowClientHeader")]
        public bool AllowClientHeader { get; set; }
    }

    /// <summary>
    /// A client endpoint behavior that authenticates the device principal using the X.509 client certificate
    /// </summary>
    [DisplayName("Client Certificate Device Authorization")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ClientCertificateAccessBehavior : IServicePolicy, IServiceBehavior
    {

        // Client certificate DN if passed from proxy
        public const string X_CLIENT_CERT_PEM = "X-SSL-ClientCert";

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(ClientCertificateAccessBehavior));

        // Trusted issuers
        private readonly ClientCertificateAccessConfiguration m_configuration = new ClientCertificateAccessConfiguration();

        /// <summary>
        /// Creates a new instance with specified configuration object
        /// </summary>
        public ClientCertificateAccessBehavior(XElement xe)
        {
            if (xe == null)
                throw new InvalidOperationException("Missing ClientCertificateAccessConfiguration");
            using (var sr = new StringReader(xe.ToString()))
                this.m_configuration = XmlModelSerializerFactory.Current.CreateSerializer(typeof(ClientCertificateAccessConfiguration)).Deserialize(sr) as ClientCertificateAccessConfiguration;

        }

        /// <summary>
        /// Creates new instance with default behavior
        /// </summary>
        public ClientCertificateAccessBehavior()
        {
            try
            {
                using (var st = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine))
                {
                    foreach (var c in st.Certificates)
                    {
                        this.m_tracer.TraceInfo("Will trust HTTPS client authentication from {0}", c.Subject);
                        this.m_configuration.TrustedIssuers.Add(new X509ConfigurationElement(c)
                        {
                            StoreName = StoreName.CertificateAuthority,
                            StoreLocation = StoreLocation.LocalMachine
                        });
                    }
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching trusted certificate authorities for clients - {0}", e);
            }
        }

        /// <summary>
        /// Apply the behavior
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                if(!request.Url.Scheme.Equals("https") || !request.IsSecure)
                {
                    throw new SecurityException("Invalid Scheme - expected https");
                }
                else if(request.ClientCertificateError != 0)
                {
                    throw new AuthenticationException($"Client Certificate Error {request.ClientCertificateError}");
                }

                IPrincipal authenticationPrincipal = null;
                var certificateIdentityService = ApplicationServiceContext.Current.GetService<ICertificateIdentityProvider>();
                if(certificateIdentityService == null)
                {
                    throw new InvalidOperationException("In order to use node authentication with X509 certificates a CertificateIdentityProvider must be configured");
                }

                var clientCertificate = request.ClientCertificate;
                var headerPem = request.Headers[X_CLIENT_CERT_PEM];

                if(!String.IsNullOrEmpty(headerPem) && this.m_configuration.AllowClientHeader)
                {
                    clientCertificate = new X509Certificate2(Encoding.UTF8.GetBytes(headerPem));
                }
                
                if(clientCertificate == null)
                {
                    throw new AuthenticationException("Client certificate required");
                }
                else {
                    // Build the chain
                    using (var chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = this.m_configuration.RevokationCheckMode;
                        chain.Build(clientCertificate);
                        if (chain.ChainStatus.Length != 0)
                        {
                            // Invalid certificate
                            this.m_tracer.TraceError("Error validating {0} from {1} - {2}", clientCertificate, RestOperationContext.Current.IncomingRequest.RemoteEndPoint, String.Join(";", chain.ChainStatus));
                            throw new AuthenticationException($"Error validating client certificate chain - ensure client certificate chain is present in request or intermediaries installed on this server's trust store");
                        }

                        // Validate that at least one level in the chain is from a trusted (as configured) issuer
                        foreach (var c in chain.ChainElements)
                        {
                            if (this.m_configuration.TrustedIssuers.Any(f => f.Certificate.Thumbprint == c.Certificate.Thumbprint))
                            {
                                authenticationPrincipal = certificateIdentityService.Authenticate(clientCertificate);

                                // Validate that there has been no shift in principal if currently authenticated
                                if (AuthenticationContext.Current.Principal is IClaimsPrincipal cp && authenticationPrincipal is IClaimsPrincipal ap)
                                {
                                    var existingIdentity = cp.Identities.FirstOrDefault(i=> i.FindFirst(SanteDBClaimTypes.Actor)?.Value == ap.FindFirst(SanteDBClaimTypes.Actor)?.Value); // find the first matching (if any) identity to validate
                                    if (existingIdentity == null)
                                    {
                                        cp.AddIdentity(authenticationPrincipal.Identity);
                                    }
                                    else if (existingIdentity.Name != authenticationPrincipal.Identity.Name)
                                    {
                                        throw new SecurityException($"Session was established on {existingIdentity.Name} but node authentication indicates {authenticationPrincipal.Identity.Name} - sessions cannot switch client nodes");
                                    }
                                }
                                else
                                {
                                    var ac = AuthenticationContext.EnterContext(authenticationPrincipal);
                                    RestOperationContext.Current.Disposed += (o, e) => ac.Dispose();
                                }
                                return;
                            }
                        }
                    }
                    // No authentication could be made = no trusted issuers
                    throw new AuthenticationException("No trusted issuer on client chain found");
                }
            }
            catch(SecurityException) { throw; }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error performing client node authentication - {0}", e);
                throw new AuthenticationException($"Error performing client node authentication", e);
            }
        }

        /// <summary>
        /// Add the policy to the dispatcher
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }
    }
}
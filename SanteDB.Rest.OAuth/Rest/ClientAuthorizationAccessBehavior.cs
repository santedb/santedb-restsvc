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
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Rest.OAuth.Rest
{
    /// <summary>
    /// Authorization policy on the OAUTH service 
    /// </summary>
    /// <remarks>
    /// Provides authentication contexts to the <see cref="OAuthServiceBehavior"/>
    /// based on the following sources of identity in the request:
    /// <list type="bullet">
    ///     <item>From the Authorization header for HTTP Basic credentials (application identity)</item>
    ///     <item>From the X-SanteDB-DeviceAuthorization header Basic credentials (device identity on non HTTPS)</item>
    /// </list>
    /// </remarks>
    [DisplayName("OAUTH: HTTP BASIC Client-Credentials")]
    [ExcludeFromCodeCoverage]
    public class ClientAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {
        // Configuration from main SanteDB
        private ApplicationServiceContextConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<ApplicationServiceContextConfigurationSection>();

        // Trace source
        private readonly Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        /// <summary>
        /// Apply the policy to the request
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                m_traceSource.TraceInfo("Entering OAuth BasicAuthorizationAccessPolicy");

                // Role service
                var appIdentityService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
                var deviceIdentityService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();

                var authHeader = request.Headers["Authorization"];
                if (!String.IsNullOrEmpty(authHeader))
                {
                    if (this.ExtractBasicAuthorizationData(authHeader, out var identifier, out var secret))
                    {
                        var principal = deviceIdentityService.Authenticate(identifier, secret);
                        if (principal == null)
                        {
                            throw new AuthenticationException("Invalid device credentials");
                        }

                        this.AppendPrincipalToAuthContext(principal);
                        RestOperationContext.Current.Data.Add("symm_secret", secret);
                    }
                }

                // Disposed context so reset the auth
            }
            catch (Exception e)
            {
                m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
            }
        }

        /// <summary>
        /// Extract from HTTP basic header
        /// </summary>
        private bool ExtractBasicAuthorizationData(string authHeader, out string identifier, out string secret)
        {

            if (string.IsNullOrEmpty(authHeader) ||
                !authHeader.ToLowerInvariant().StartsWith("basic"))
            {
                identifier = secret = null;
                return false;
            }

            authHeader = authHeader.Substring(6);
            var b64Data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader)).Split(':');
            if (b64Data.Length != 2)
            {
                throw new SecurityException("Malformed HTTP Basic Header");
            }
            identifier = b64Data[0];
            secret = b64Data[1];
            return true;
        }

        /// <summary>
        /// Append principal
        /// </summary>
        private void AppendPrincipalToAuthContext(IPrincipal principal)
        {
            // If the current principal is set-up then add the identity if not then don't
            if (AuthenticationContext.Current.Principal == AuthenticationContext.AnonymousPrincipal)
            {
                var contextToken = AuthenticationContext.EnterContext(principal);
                RestOperationContext.Current.Disposed += (o, e) => contextToken.Dispose();
            }
            else
            {
                (AuthenticationContext.Current.Principal as IClaimsPrincipal).AddIdentity(principal.Identity);
            }
        }

        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }
    }
}
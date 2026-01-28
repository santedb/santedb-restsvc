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
 * Date: 2024-12-12
 */
using RestSrvr.Message;
using RestSrvr;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core;
using SanteDB.Rest.Common.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Security.Authentication;
using System.Security;
using System.Text;
using System.Linq;
using SanteDB.Core.Model.Security;

namespace SanteDB.Rest.Common.Security
{
    /// <summary>
    /// Basic authorization policy
    /// </summary>
    [DisplayName("HTTP BASIC Authentication using Application Credentials")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class BasicApplicationAuthorizationAccessBehavior : IAuthorizationServicePolicy, IServiceBehavior
    {
        // Configuration from main SanteDB
        private BasicAuthorizationConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<BasicAuthorizationConfigurationSection>();

        // Trace source
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(BasicApplicationAuthorizationAccessBehavior));

        /// <summary>
        /// Apply the policy to the request
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            IDisposable context = null;
            try
            {
                this.m_traceSource.TraceInfo("Entering BasicAuthorizationAccessPolicy");

                // Role service
                var roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
                var appIdentityService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
                var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
                var pdpService = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();

                var httpRequest = RestOperationContext.Current.IncomingRequest;

                var authHeader = httpRequest.Headers["Authorization"];
                if (String.IsNullOrEmpty(authHeader) ||
                    !authHeader.ToLowerInvariant().StartsWith("basic"))
                {
                    throw new AuthenticationException("Invalid authentication scheme");
                }

                authHeader = authHeader.Substring("basic ".Length);
                var b64Data = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader)).Split(':');
                if (b64Data.Length != 2)
                {
                    throw new SecurityException("Malformed HTTP Basic Header");
                }

                var principal = appIdentityService.Authenticate(b64Data[0], b64Data[1]);
                if (principal == null)
                {
                    throw new AuthenticationException("Invalid username/password");
                }

                // Add claims made by the client
                var claims = new List<IClaim>();
                if (principal is IClaimsPrincipal)
                {
                    claims.AddRange((principal as IClaimsPrincipal).Claims);
                }

                var clientClaims = httpRequest.Headers.ExtractClientClaims();
                foreach (var claim in clientClaims)
                {
                    if (this.m_configuration?.AllowedClientClaims?.Contains(claim.Type) == false)
                    {
                        throw new SecurityException("Claim not allowed");
                    }
                    else
                    {
                        var handler = claim.GetHandler();
                        if (handler == null ||
                            handler.Validate(principal, claim.Value))
                        {
                            claims.Add(claim);
                        }
                        else
                        {
                            throw new SecurityException("Claim validation failed");
                        }
                    }
                }

                // Claim headers built in
                if (pipService != null)
                {
                    claims.AddRange(pdpService.GetEffectivePolicySet(principal).Where(o => o.Rule == PolicyGrantType.Grant).Select(o => new SanteDBClaim(SanteDBClaimTypes.SanteDBGrantedPolicyClaim, o.Policy.Oid)));
                }

                //We are a client so extra validation is not required.
                // Finally validate the client
                //var claimsPrincipal = new SanteDBClaimsPrincipal(new SanteDBClaimsIdentity(principal.Identity, claims));

                //if (this.m_configuration?.RequireClientAuth == true)
                //{
                //    var clientId = httpRequest.Headers[ExtendedHttpHeaderNames.BasicHttpClientIdHeaderName];
                //    if (clientId == null)
                //    {
                //        throw new SecurityException("Missing ClientId");
                //    }
                //    else
                //    {
                //        var applicationPrincipal = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(clientId, claimsPrincipal);
                //        claimsPrincipal.AddIdentity(applicationPrincipal.Identity as IClaimsIdentity);
                //    }
                //}

                AuthenticationContext.EnterContext(principal);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
            }
            finally
            {
                // Disposed context so reset the auth
                RestOperationContext.Current.Disposed += (o, e) =>
                {
                    AuthenticationContext.Current.Abandon();
                };
            }
        }

        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }

        /// <summary>
        /// Add authentication challenge header
        /// </summary>
        public void AddAuthenticateChallengeHeader(RestResponseMessage faultMessage, Exception error)
        {
            this.AddWwwAuthenticateHeader("basic", error, faultMessage);

        }
    }

}

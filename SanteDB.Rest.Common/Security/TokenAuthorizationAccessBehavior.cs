/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Security.Principal;

namespace SanteDB.Rest.Common.Security
{
    /// <summary>
    /// Token authorization access behavior
    /// </summary>
    [DisplayName("BEARER Token Authorization")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class TokenAuthorizationAccessBehavior : IAuthorizationServicePolicy, IServiceBehavior
    {
        /// <summary>
        /// Gets the session property name
        /// </summary>
        public const string RestPropertyNameSession = "Session";

        // Trace source
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(TokenAuthorizationAccessBehavior));

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <returns>True if authorization is successful</returns>
        private IDisposable CheckBearerAccess(string authorizationToken)
        {

            var session = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>().GetSessionFromIdToken(authorizationToken);

            IPrincipal principal = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session);
            if (principal == null)
                throw new SecuritySessionException(SessionExceptionType.Other, "Invalid bearer token", null);

            RestOperationContext.Current.Data.Add(RestPropertyNameSession, session);

            this.m_traceSource.TraceInfo("User {0} authenticated via SESSION BEARER", principal.Identity.Name);
            return AuthenticationContext.EnterContext(principal);
        }

        /// <summary>
        /// Apply the authorization policy rule
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                this.m_traceSource.TraceInfo("CheckAccess");

                // Http message inbound
                var httpMessage = RestOperationContext.Current.IncomingRequest;

                // Get the authorize header
                String authorization = httpMessage.Headers["Authorization"];
                if (authorization == null)
                {
                    if (httpMessage.HttpMethod == "OPTIONS" || httpMessage.HttpMethod == "PING")
                    {
                        return;
                    }
                    else
                        throw new SecuritySessionException(SessionExceptionType.NotEstablished, "Missing Authorization header", null);
                }

                // Authorization method
                var auth = authorization.Split(' ').Select(o => o.Trim()).ToArray();
                switch (auth[0].ToLowerInvariant())
                {
                    case "bearer":
                        var contextToken = this.CheckBearerAccess(auth[1]);
                        RestOperationContext.Current.Disposed += (o, e) => contextToken.Dispose();
                        break;

                    default:
                        throw new SecuritySessionException(SessionExceptionType.TokenType, "Invalid authentication scheme", null);
                }
            }
            catch (SecuritySessionException e)
            {
                AuditUtil.AuditNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null);
                throw;
            }
            catch (UnauthorizedAccessException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                AuditUtil.AuditNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null);
                throw;
            }
            catch (KeyNotFoundException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                AuditUtil.AuditNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null);
                throw new SecuritySessionException(SessionExceptionType.NotEstablished, e.Message, e);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                AuditUtil.AuditNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null);
                throw new SecuritySessionException(SessionExceptionType.Other, e.Message, e);
            }
        }

        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }

        /// <inheritdoc cref="IAuthorizationServicePolicy.AddAuthenticateChallengeHeader(RestResponseMessage, Exception)"/>
        public void AddAuthenticateChallengeHeader(RestResponseMessage faultMessage, Exception error)
        {
            // Map error codes to headers according to https://www.rfc-editor.org/rfc/rfc6750#section-3
            switch (error)
            {
                case PolicyViolationException pve:
                    faultMessage.AddAuthenticateHeader("bearer", RestOperationContext.Current.IncomingRequest.Url.Host, "insufficient_scope", pve.PolicyId, pve.Message);
                    break;
                case SecuritySessionException sse:
                    switch(sse.Type)
                    {
                        case SessionExceptionType.Scope:
                            faultMessage.AddAuthenticateHeader("bearer", RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_scope", description: sse.Message);
                            break;
                        default:
                            faultMessage.AddAuthenticateHeader("bearer", RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_token", description: sse.Message);
                            break;
                    }
                    break;
                default:
                    faultMessage.AddAuthenticateHeader("bearer", RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_request", description: error.Message);
                    break;
            }
        }
    }
}
/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Matching;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        /// Delegate to resolve a session from a bearer token.
        /// </summary>
        private static Func<string, ISession> s_SessionResolverDelegate;

        static TokenAuthorizationAccessBehavior()
        {
            s_SessionResolverDelegate = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>().GetSessionFromBearerToken;
        }

        /// <summary>
        /// Sets the session resolution delegate for all instances of the <see cref="TokenAuthorizationAccessBehavior"/>.
        /// </summary>
        /// <param name="sessionResolverDelegate"></param>
        public static void SetSessionResolverDelegate(Func<string, ISession> sessionResolverDelegate)
        {
            s_SessionResolverDelegate = sessionResolverDelegate;
        }

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <returns>True if authorization is successful</returns>
        private IDisposable CheckBearerAccess(string authorizationToken)
        {
            //var session = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>().GetSessionFromBearerToken(authorizationToken);

            var session = s_SessionResolverDelegate(authorizationToken);

            if (session == null)
            {
                throw new SecuritySessionException(SessionExceptionType.Other, "Invalid bearer token", null);
            }
            IPrincipal principal = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session);
            if (principal == null)
            {
                throw new SecuritySessionException(SessionExceptionType.Other, "Invalid bearer token", null);
            }

            RestOperationContext.Current.Data.Add(RestPropertyNameSession, session);

            this.m_traceSource.TraceVerbose("User {0} authenticated via SESSION BEARER", principal.Identity.Name);
            return AuthenticationContext.EnterContext(principal);
        }

        /// <summary>
        /// Apply the authorization policy rule
        /// </summary>
        public void Apply(RestRequestMessage request)
        {
            try
            {

                // Http message inbound
                var httpMessage = RestOperationContext.Current.IncomingRequest;

                // Get the authorize header
                String authorization = httpMessage.Headers["Authorization"];
                if (authorization == null)
                {
                    if (!RestOperationContext.Current.AppliedPolicies.OfType<IAuthorizationServicePolicy>().Any() &&
                        !AuthenticationContext.Current.Principal.Equals(AuthenticationContext.AnonymousPrincipal))
                    {
                        AuthenticationContext.Current?.Abandon();
                    }
                    this.m_traceSource.TraceVerbose("Request {0} has no authorization header - skipping", httpMessage.Url);
                    return;
                }

                // Authorization method
                var auth = authorization.Split(' ').Select(o => o.Trim()).ToArray();
                switch (auth[0].ToLowerInvariant())
                {
                    case "bearer":
                        var contextToken = this.CheckBearerAccess(auth[1]);
                        RestOperationContext.Current.Disposed += (o, e) =>
                        {
                            contextToken.Dispose();
                            AuthenticationContext.Current.Abandon();
                        };
                        break;

                    default:
                        return;
                }
            }
            catch (SecuritySessionException e)
            {
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null).Send();
                throw;
            }
            catch (UnauthorizedAccessException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null).Send();
                throw;
            }
            catch (KeyNotFoundException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null).Send();
                throw new SecuritySessionException(SessionExceptionType.NotEstablished, e.Message, e);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(e, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, null).Send();
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
            this.AddWwwAuthenticateHeader("bearer", error, faultMessage);
        }
    }
}
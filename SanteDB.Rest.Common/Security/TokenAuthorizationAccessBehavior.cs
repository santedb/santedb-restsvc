/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
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
    public class TokenAuthorizationAccessBehavior : IServicePolicy, IServiceBehavior
    {

        /// <summary>
        /// Gets the session property name
        /// </summary>
        public const string RestPropertyNameSession = "Session";

        // Trace source
        private Tracer m_traceSource = Tracer.GetTracer(typeof(TokenAuthorizationAccessBehavior));

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <returns>True if authorization is successful</returns>
        private IDisposable CheckBearerAccess(string authorizationToken)
        {
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(
                Enumerable.Range(0, authorizationToken.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(authorizationToken.Substring(x, 2), 16))
                                    .ToArray()
            );

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
            catch (UnauthorizedAccessException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                throw;
            }
            catch (KeyNotFoundException e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
                throw new SecuritySessionException(SessionExceptionType.NotEstablished, e.Message, e);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Token Error (From: {0}) : {1}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint, e);
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
    }
}

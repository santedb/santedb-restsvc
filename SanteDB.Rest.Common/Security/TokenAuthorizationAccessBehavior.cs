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
using System.Text;

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
                        RestOperationContext.Current.Disposed += (o,e) => contextToken.Dispose();
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

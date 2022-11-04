using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Security;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// An authentication behavior which establishes a current session using cookies from the client
    /// </summary>
    public class CookieAuthenticationBehavior : IServiceBehavior, IAuthorizationServicePolicy
    {

        public const string RestPropertyNameSession = "Session";

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CookieAuthenticationBehavior));

        // Applet collection to use
        private readonly ReadonlyAppletCollection m_appletCollection;

        // Session token resolver
        private readonly ISessionTokenResolverService m_sessionTokenResolver;
        private readonly ISessionIdentityProviderService m_sessionIdentityProvider;
        private readonly ISymmetricCryptographicProvider m_symmetricProvider;

        /// <summary>
        /// Applet collection
        /// </summary>
        public CookieAuthenticationBehavior()
        {
            var defaultSolution = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AppletConfigurationSection>()?.DefaultSolution;
            if (!string.IsNullOrEmpty(defaultSolution))
            {
                m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>()?.GetApplets(defaultSolution);
            }
            if (defaultSolution == null || m_appletCollection == null) // No default solution - or there was no solution manager
            {
                m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets;
            }
            m_sessionTokenResolver = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>();
            m_sessionIdentityProvider = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
            m_symmetricProvider = ApplicationServiceContext.Current.GetService<ISymmetricCryptographicProvider>();
        }

        /// <inheritdoc cref="IAuthorizationServicePolicy.AddAuthenticateChallengeHeader(RestResponseMessage, Exception)"/>
        public void AddAuthenticateChallengeHeader(RestResponseMessage faultMessage, Exception error)
        {
            // We want to redirect to the login provider
            var loginAsset = m_appletCollection.GetLoginAssetPath();
            if (!string.IsNullOrEmpty(loginAsset))
            {
                faultMessage.StatusCode = System.Net.HttpStatusCode.Redirect;
                faultMessage.Headers.Add("Location", loginAsset);
            }
            else
            {
                faultMessage.AddAuthenticateHeader("cookie", RestOperationContext.Current.IncomingRequest.Url.Host);
            }
        }

        /// <inheritdoc cref="IServicePolicy.Apply(RestRequestMessage)"/>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                var authCookie = request.Cookies["_s"];
                if (AuthenticationContext.Current.Principal.Identity.Name == AuthenticationContext.AnonymousPrincipal.Identity.Name &&
                    authCookie != null)
                {
                    var session = m_sessionTokenResolver.GetSessionFromIdToken(m_symmetricProvider.Decrypt(authCookie.Value));
                    if (session != null)
                    {

                        RestOperationContext.Current.Data.Add(RestPropertyNameSession, session);

                        var authContext = AuthenticationContext.EnterContext(m_sessionIdentityProvider.Authenticate(session));
                        RestOperationContext.Current.Disposed += (o, e) => authContext.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                RestOperationContext.Current.OutgoingResponse.SetCookie(new System.Net.Cookie("_s", "")
                {
                    Discard = true,
                    Expired = true,
                    Expires = DateTime.Now
                });
                throw;
            }
        }

        /// <inheritdoc cref="IServiceBehavior.ApplyServiceBehavior(RestService, ServiceDispatcher)"/>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);

        }
    }
}

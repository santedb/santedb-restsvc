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

namespace SanteDB.Rest.WWW.Behaviors
{
    /// <summary>
    /// An authentication behavior which establishes a current session using cookies from the client
    /// </summary>
    public class CookieAuthenticationBehavior : IServiceBehavior, IAuthorizationServicePolicy
    {


        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CookieAuthenticationBehavior));

        // Applet collection to use
        private readonly ReadonlyAppletCollection m_appletCollection;

        // Session token resolver
        private readonly ISessionTokenResolverService m_sessionTokenResolver;
        private readonly ISessionIdentityProviderService m_sessionIdentityProvider;

        /// <summary>
        /// Applet collection
        /// </summary>
        public CookieAuthenticationBehavior()
        {
            var defaultSolution = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AppletConfigurationSection>()?.DefaultSolution;
            if (!String.IsNullOrEmpty(defaultSolution))
            {
                this.m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().GetApplets(defaultSolution);
            }
            if (defaultSolution == null)
            {
                this.m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets;
            }
            this.m_sessionTokenResolver = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>();
            this.m_sessionIdentityProvider = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
        }

        /// <inheritdoc cref="IAuthorizationServicePolicy.AddAuthenticateChallengeHeader(RestResponseMessage, Exception)"/>
        public void AddAuthenticateChallengeHeader(RestResponseMessage faultMessage, Exception error)
        {
            // We want to redirect to the login provider
            var loginAsset = this.m_appletCollection.GetLoginAssetPath();
            if (!String.IsNullOrEmpty(loginAsset))
            {
                faultMessage.StatusCode = System.Net.HttpStatusCode.Redirect;
                faultMessage.Headers.Add("Location", loginAsset);
            }
        }

        /// <inheritdoc cref="IServicePolicy.Apply(RestRequestMessage)"/>
        public void Apply(RestRequestMessage request)
        {
            try
            {
                var authCookie = request.Cookies["_s"];
                if (authCookie != null)
                {
                    var session = this.m_sessionTokenResolver.GetSessionFromIdToken(authCookie.Value);
                    var authContext = AuthenticationContext.EnterContext(this.m_sessionIdentityProvider.Authenticate(session));
                    RestOperationContext.Current.Disposed += (o, e) => authContext.Dispose();
                }
            }
            catch (SecurityException)
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

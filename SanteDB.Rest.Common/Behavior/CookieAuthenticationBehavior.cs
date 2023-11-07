/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
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

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// An authentication behavior which establishes a current session using cookies from the client
    /// </summary>
    public class CookieAuthenticationBehavior : IServiceBehavior, IAuthorizationServicePolicy
    {

        /// <summary>
        /// The key for the <see cref="ISession"/> stored in the <see cref="RestOperationContext"/> by the <see cref="CookieAuthenticationBehavior"/>.
        /// </summary>
        public const string RestDataItem_Session = "Session";

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
                var authCookie = request.Cookies[ExtendedCookieNames.SessionCookieName];
                if (authCookie != null)
                {
                    var session = m_sessionTokenResolver.GetSessionFromBearerToken(authCookie.Value);
                    if (session != null)
                    {

                        RestOperationContext.Current.Data.Add(RestDataItem_Session, session);

                        var authContext = AuthenticationContext.EnterContext(m_sessionIdentityProvider.Authenticate(session));
                        RestOperationContext.Current.Disposed += (o, e) => authContext.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                RestOperationContext.Current.OutgoingResponse.SetCookie(new System.Net.Cookie(ExtendedCookieNames.SessionCookieName, "")
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

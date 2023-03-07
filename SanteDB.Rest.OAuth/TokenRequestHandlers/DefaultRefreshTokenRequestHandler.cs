using Newtonsoft.Json.Serialization;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.OAuth.TokenRequestHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultRefreshTokenRequestHandler : ITokenRequestHandler
    {
        readonly ISessionTokenResolverService _SessionResolver;
        readonly ISessionIdentityProviderService _SessionIdentityProvider;
        readonly ISessionProviderService _SessionProvider;
        readonly IAuditService _AuditService;
        readonly Tracer _Tracer;

        /// <summary>
        /// Constructs a new instance of the class.
        /// </summary>
        /// <param name="sessionResolver">Injected through dependency injection.</param>
        /// <param name="sessionIdentityProvider"></param>
        public DefaultRefreshTokenRequestHandler(ISessionTokenResolverService sessionResolver, ISessionIdentityProviderService sessionIdentityProvider, ISessionProviderService sessionProvider, IAuditService auditService)
        {
            _Tracer = new Tracer(nameof(DefaultRefreshTokenRequestHandler));
            _SessionResolver = sessionResolver;
            _SessionIdentityProvider = sessionIdentityProvider;
            _SessionProvider = sessionProvider;
            _AuditService = auditService;
        }

        /// <inheritdoc/>
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameRefresh };

        /// <inheritdoc/>
        public string ServiceName => "Default Refresh Token Request Handler";

        /// <inheritdoc/>
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.RefreshToken))
            {
                _Tracer.TraceVerbose("Context contains empty refresh token");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing refresh token";
                return false;
            }

            try
            {

                var appidentity = context.GetApplicationIdentity();

                if (null == appidentity)
                {
                    _Tracer.TraceInfo("No application identity provided in the reuqest.");
                    context.ErrorType = OAuthErrorType.invalid_request;
                    context.ErrorMessage = "missing client_id";
                    return false;
                }

                context.Session = _SessionResolver.ExtendSessionWithRefreshToken(context.RefreshToken);

                var principal = _SessionIdentityProvider.Authenticate(context.Session) as IClaimsPrincipal;

                var sessionapp = principal?.Identities?.OfType<IApplicationIdentity>()?.FirstOrDefault() as IClaimsIdentity;

                if (sessionapp.Name != appidentity.Name)
                {
                    //Abandon the session because the request is invalid.
                    _SessionProvider.Abandon(context.Session);
                    context.Session = null;
                    _Tracer.TraceWarning("OAuth reuqest contains a refresh token for a different application than the request was sent from.");
                    context.ErrorType = OAuthErrorType.invalid_client;
                    context.ErrorMessage = "invalid refresh token";
                    return false;
                }

                _AuditService.Audit().ForSessionStart(context.Session, principal, null != context.Session).Send();

                if (null == context.Session)
                {
                    _Tracer.TraceInfo("Failed to initialize session from refresh token.");
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "invalid refresh token";
                    return false;
                }

                return true;
            }
            catch (SecuritySessionException ex)
            {
                _Tracer.TraceInfo("Failed to initialize session from refresh token. {0}", ex.ToString());

                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid refresh token";
                return false;
            }
        }
    }
}

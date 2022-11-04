using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System.Collections.Generic;
using System.Security.Authentication;

namespace SanteDB.Rest.OAuth.TokenRequestHandlers
{
    /// <summary>
    /// Token request handler for the password grant type. This handler will return a response with a user principal.
    /// </summary>
    public class DefaultPasswordTokenRequestHandler : ITokenRequestHandler
    {
        readonly IPolicyEnforcementService _PolicyEnforcementService;
        readonly IApplicationIdentityProviderService _ApplicationIdentityProviderService;
        readonly Tracer _Tracer;

        /// <summary>
        /// Constructs a new instance of the handler.
        /// </summary>
        /// <param name="policyEnforcementService"></param>
        /// <param name="applicationIdentityProviderService"></param>
        public DefaultPasswordTokenRequestHandler(IPolicyEnforcementService policyEnforcementService, IApplicationIdentityProviderService applicationIdentityProviderService)
        {
            _Tracer = new Tracer(nameof(DefaultPasswordTokenRequestHandler));
            _PolicyEnforcementService = policyEnforcementService;
            _ApplicationIdentityProviderService = applicationIdentityProviderService;

        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNamePassword };

        /// <inheritdoc />
        public string ServiceName => "Default Password Token Request Handler";

        /// <inheritdoc />
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.Username))
            {
                _Tracer.TraceInfo("Missing username in Token request.");
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "missing username in the request";
                return false;
                //return CreateErrorCondition(OAuthErrorType.invalid_request, "missing username in the request.");
            }

            try
            {
                var identityprovider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

                if (!string.IsNullOrEmpty(context.TfaSecret))
                {
                    context.UserPrincipal = identityprovider.Authenticate(context.Username, context.Password, context.TfaSecret) as IClaimsPrincipal;
                }
                else
                {
                    context.UserPrincipal = identityprovider.Authenticate(context.Username, context.Password) as IClaimsPrincipal;
                }

                // The principal is already a token based principal - so we may have gotten it from an upstream - we just have to relay this token back to the caller 
                if(context.UserPrincipal is ITokenPrincipal itp)
                {
                    context.IdToken = itp.IdentityToken;
                    context.AccessToken = itp.AccessToken;
                    context.TokenType = itp.TokenType;
                    return true;
                }
                else if (null != context.UserPrincipal)
                {
                    _PolicyEnforcementService.Demand(OAuthConstants.OAuthPasswordFlowPolicy, context.UserPrincipal);

                    if (null == context.DevicePrincipal)
                    {
                        _PolicyEnforcementService.Demand(OAuthConstants.OAuthPasswordFlowPolicyWithoutDevice, context.UserPrincipal);
                    }

                    //Try to on-behalf-of the application identity with our user principal.
                    if (context.UserPrincipal?.Identity?.IsAuthenticated == true && null == context.ApplicationPrincipal)
                    {
                        var app = _ApplicationIdentityProviderService.Authenticate(context?.ClientId, context.UserPrincipal);

                        if (null != app && app.Identity is IApplicationIdentity)
                        {
                            context.ApplicationIdentity = app.Identity as IClaimsIdentity;
                            context.ApplicationPrincipal = app as IClaimsPrincipal;
                        }
                    }
                } 

                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicy, context.ApplicationPrincipal);

                if (null != context.DevicePrincipal)
                {
                    _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicy, context.DevicePrincipal);
                }
                else
                {
                    _PolicyEnforcementService?.Demand(OAuthConstants.OAuthPasswordFlowPolicyWithoutDevice, context.ApplicationPrincipal);
                }
            }
            catch (AuthenticationException authnex)
            {
                _Tracer.TraceWarning("Exception during IIdentityProvider.Authenticate(): {0}", authnex.ToString());
                context.ErrorMessage = authnex.Message;
                context.ErrorType = OAuthErrorType.invalid_grant;
                return false;
            }

            if (null == context.UserPrincipal)
            {
                _Tracer.TraceInfo("Authentication failed in Token request.");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid password";
                return false;
            }

            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;
        }
    }
}

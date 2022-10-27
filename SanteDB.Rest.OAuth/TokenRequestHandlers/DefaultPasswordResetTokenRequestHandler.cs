using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;

namespace SanteDB.Rest.OAuth.TokenRequestHandlers
{
    /// <summary>
    /// Default implementation of the x_challenge grant token request handler
    /// </summary>
    public class DefaultPasswordResetTokenRequestHandler : ITokenRequestHandler
    {
        readonly Tracer _Tracer;
        readonly IPolicyEnforcementService _PolicyService;
        readonly ISecurityChallengeIdentityService _SecurityChallengeService;
        readonly IApplicationIdentityProviderService _ApplicationIdentityProviderService;

        /// <summary>
        /// Constructs a new instance of the handler.
        /// </summary>
        /// <param name="policyService"></param>
        /// <param name="securityChallengeService"></param>
        public DefaultPasswordResetTokenRequestHandler(IPolicyEnforcementService policyService, ISecurityChallengeIdentityService securityChallengeService, IApplicationIdentityProviderService applicationIdentityProviderService)
        {
            _Tracer = new Tracer(nameof(DefaultPasswordResetTokenRequestHandler));
            _PolicyService = policyService;
            _SecurityChallengeService = securityChallengeService;
            _ApplicationIdentityProviderService = applicationIdentityProviderService;   
        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameReset };

        /// <inheritdoc />
        public string ServiceName => "Default Password Reset Token Request Handler";

        /// <inheritdoc />
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            //Validate the request.
            if (string.IsNullOrEmpty(context?.Username))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid username";
                return false;
            }

            if (string.IsNullOrEmpty(context?.SecurityChallenge) || !Guid.TryParse(context?.SecurityChallenge, out var securitychallengeguid))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid challenge";
                return false;
            }

            if (string.IsNullOrEmpty(context?.SecurityChallengeResponse))
            {
                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "invalid response";
                return false;
            }

            //Forcibly set the scope to be login only. This prevents the user from making any modifications to the system after they have updated the password.
            context.Scopes = context.Scopes ?? new List<string>();
            context.Scopes.Clear();
            context.Scopes.Add(PermissionPolicyIdentifiers.LoginPasswordOnly);

            try
            {
                context.UserPrincipal = _SecurityChallengeService.Authenticate(context.Username, securitychallengeguid, context.SecurityChallengeResponse, context.TfaSecret) as IClaimsPrincipal;

                if (null != context.UserPrincipal)
                {
                    _PolicyService.Demand(OAuthConstants.OAuthResetFlowPolicy, context.UserPrincipal);

                    if (null == context.DevicePrincipal)
                    {
                        _PolicyService.Demand(OAuthConstants.OAuthResetFlowPolicyWithoutDevice, context.UserPrincipal);
                    }

                    if (context.UserPrincipal?.Identity?.IsAuthenticated == true && null == context.ApplicationPrincipal)
                    {
                        var app = _ApplicationIdentityProviderService.Authenticate(context.ClientId, context.UserPrincipal);

                        if (null != app && app.Identity is IApplicationIdentity)
                        {
                            context.ApplicationIdentity = app.Identity as IClaimsIdentity;
                            context.ApplicationPrincipal = app as IClaimsPrincipal;
                        }
                    }

                    _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicy, context.ApplicationPrincipal);

                    if (null != context.DevicePrincipal)
                    {
                        _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicy, context.DevicePrincipal);
                    }
                    else
                    {
                        _PolicyService?.Demand(OAuthConstants.OAuthResetFlowPolicyWithoutDevice, context.ApplicationPrincipal);
                    }
                }
            }
            catch (AuthenticationException authnex)
            {
                _Tracer.TraceWarning("Exception during ISecurityChallengeIdentityService.Authenticate(): {0}", authnex.ToString());
                context.ErrorMessage = authnex.Message;
                context.ErrorType = OAuthErrorType.invalid_grant;
                return false;
            }

            if (null == context.UserPrincipal)
            {
                _Tracer.TraceInfo("Authentication failed in Token request.");
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid challenge response";
                return false;
            }
            

            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;

        }

    }
}

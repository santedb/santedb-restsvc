/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System.Collections.Generic;
using System.Security.Authentication;

#pragma warning disable CS0618
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
                if (context.UserPrincipal is ITokenPrincipal itp)
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
            catch (TfaRequiredAuthenticationException tfareqex)
            {
                _Tracer.TraceVerbose("Authentication failed due to Tfa configured on account.");
                context.ErrorMessage = tfareqex.Message;
                context.ErrorType = OAuthErrorType.mfa_required;
                return false;
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
#pragma warning restore
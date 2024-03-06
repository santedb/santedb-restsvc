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
using Newtonsoft.Json;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.OAuth.TokenRequestHandlers
{
    /// <summary>
    /// Token request handler for the authorization_code grant. 
    /// </summary>
    public class DefaultAuthorizationCodeTokenRequestHandler : ITokenRequestHandler
    {
        readonly Tracer _Tracer = new Tracer(nameof(DefaultAuthorizationCodeTokenRequestHandler));

        readonly IPolicyEnforcementService _PolicyEnforcementService;
        readonly IApplicationIdentityProviderService _ApplicationIdentityProvider;
        readonly IDeviceIdentityProviderService _DeviceIdentityProvider;
        readonly IIdentityProviderService _IdentityProvider;
        readonly ISymmetricCryptographicProvider _SymmetricProvider;

        readonly TimeSpan _AuthorizationCodeValidityPeriod = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Constructs a new instance of the handler.
        /// </summary>
        /// <param name="policyEnforcementService"></param>
        /// <param name="applicationIdentityProvider"></param>
        /// <param name="deviceIdentityProvider"></param>
        /// <param name="identityProvider"></param>
        /// <param name="symmetricProvider"></param>
        public DefaultAuthorizationCodeTokenRequestHandler(IPolicyEnforcementService policyEnforcementService, IApplicationIdentityProviderService applicationIdentityProvider, IDeviceIdentityProviderService deviceIdentityProvider, IIdentityProviderService identityProvider, ISymmetricCryptographicProvider symmetricProvider)
        {
            _PolicyEnforcementService = policyEnforcementService;
            _ApplicationIdentityProvider = applicationIdentityProvider;
            _DeviceIdentityProvider = deviceIdentityProvider;
            _IdentityProvider = identityProvider;
            _SymmetricProvider = symmetricProvider;
        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameAuthorizationCode };

        /// <inheritdoc />
        public string ServiceName => "Default Authorization Code Token Request Handler";

        /// <inheritdoc />
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context.AuthorizationCode))
            {
                _Tracer.TraceInfo("{0}: Missing Authorization Code in Token request.", context.IncomingRequest.RequestTraceIdentifier);

                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "missing authorization_code";
                return false;
            }

            var authcode = DecodeAndValidateAuthorizationCode(context.AuthorizationCode);

            if (null == authcode)
            {
                _Tracer.TraceInfo("{0}: Null Authorization code. This can happen if the code has been tampered with and decryption fails.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid authorization_code";
                return false;
            }

            if (DateTimeOffset.UtcNow - authcode.iat > _AuthorizationCodeValidityPeriod)
            {
                _Tracer.TraceInfo("{0}: Expired Authorization Code.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "expired authorization_code";
                return false;
            }


            if (null != authcode.dev)
            {
                if (null == context.DeviceIdentity)
                {
                    _Tracer.TraceInfo("{0}: Auth Code was generated with a device identity but device did not authenticate the token request.", context.IncomingRequest.RequestTraceIdentifier);
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "missing device identity";
                    return false;
                }

                if (!authcode.dev.Equals(context.DeviceIdentity.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.SecurityId)?.Value))
                {
                    _Tracer.TraceWarning("{0}: Auth Code device does not match authenticated device in token request.", context.IncomingRequest.RequestTraceIdentifier);
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "mismatch device identity";
                    return false;
                }

                if (null == context.DevicePrincipal)
                {
                    context.DevicePrincipal = new SanteDBClaimsPrincipal(context.DeviceIdentity);
                }
            }
            else if (context.DeviceIdentity != null)
            {
                _Tracer.TraceInfo("{0}: Device was authenticated for token request but not for authorize request.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing device identity";
                return false;
            }

            if (null != authcode.app)
            {
                if (null == context.ApplicationIdentity)
                {
                    _Tracer.TraceInfo("{0}: Auth code was generated with an app identity but no app identity is present on token request.", context.IncomingRequest.RequestTraceIdentifier);
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "missing client identity";
                    return false;
                }

                if (!authcode.app.Equals(context.ApplicationIdentity.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.SecurityId)?.Value))
                {
                    _Tracer.TraceWarning("{0}: Auth code application identity does not match identity of token request.");
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "mismatch client identity";
                    return false;
                }
            }
            else if (context.ApplicationIdentity != null)
            {
                _Tracer.TraceInfo("{0}: No app identity is present in auth code and app identity is present on token request.");

                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing client identity";
                return false;
            }

            if (null == context.DevicePrincipal)
            {
                _Tracer.TracePolicyDemand(context.IncomingRequest.RequestTraceIdentifier, OAuthConstants.OAuthCodeFlowPolicyWithoutDevice, context.ApplicationPrincipal);
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthCodeFlowPolicyWithoutDevice, context.ApplicationPrincipal);
            }

            context.UserIdentity = _IdentityProvider.GetIdentity(Guid.Parse(authcode.usr)) as IClaimsIdentity;

            if (null == context.UserIdentity)
            {
                _Tracer.TraceWarning("{0}: No user identity was returned from the identity provider. Identity: {1}", context.IncomingRequest.RequestTraceIdentifier, authcode.usr);
                return false;
            }

            if (!context.UserIdentity.IsAuthenticated)
            {
                //Wrap so we can be authenticated
                context.UserIdentity = new SanteDBClaimsIdentity(context.UserIdentity.Name, true, "LOCAL", context.UserIdentity?.Claims);
            }

            _Tracer.TraceVerbose("{0}: Creating User principal for token generation.", context.IncomingRequest.RequestTraceIdentifier);
            context.UserPrincipal = new SanteDBClaimsPrincipal(context.UserIdentity);

            context.Nonce = authcode.nonce; //Pass the nonce back.

            context.Session = null; //Let the behaviour establish the session.
            return true;

        }

        private AuthorizationCode DecodeAndValidateAuthorizationCode(string authorizationCode)
        {
            if (string.IsNullOrEmpty(authorizationCode))
            {
                return null;
            }

            try
            {
                var json = _SymmetricProvider.Decrypt(authorizationCode);

                return JsonConvert.DeserializeObject<AuthorizationCode>(json);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}

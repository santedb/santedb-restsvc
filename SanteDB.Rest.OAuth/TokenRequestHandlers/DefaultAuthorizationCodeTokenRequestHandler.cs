﻿using Newtonsoft.Json;
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
                _Tracer.TraceInfo("Missing Authorization Code in Token request.");

                context.ErrorType = OAuthErrorType.invalid_request;
                context.ErrorMessage = "missing authorization_code";
                return false;
            }

            var authcode = DecodeAndValidateAuthorizationCode(context.AuthorizationCode);

            if (null == authcode)
            {
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid authorization_code";
                return false;
            }

            if (DateTimeOffset.UtcNow - authcode.iat > _AuthorizationCodeValidityPeriod)
            {
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "expired authorization_code";
                return false;
            }


            if (null != authcode.dev)
            {
                if (null == context.DeviceIdentity)
                {
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "missing device identity";
                    return false;
                }

                if (!authcode.dev.Equals(context.DeviceIdentity.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value))
                {
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
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing device identity";
                return false;
            }

            if (null != authcode.app)
            {
                if (null == context.ApplicationIdentity)
                {
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "missing client identity";
                    return false;
                }

                if (!authcode.app.Equals(context.ApplicationIdentity.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Sid)?.Value))
                {
                    context.ErrorType = OAuthErrorType.invalid_grant;
                    context.ErrorMessage = "mismatch client identity";
                    return false;
                }
            }
            else if (context.ApplicationIdentity != null)
            {
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "missing client identity";
            }

            if (null == context.DevicePrincipal)
            {
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthCodeFlowPolicyWithoutDevice, context.ApplicationPrincipal);
            }

            context.UserIdentity = _IdentityProvider.GetIdentity(Guid.Parse(authcode.usr)) as IClaimsIdentity;

            if (null == context.UserIdentity)
            {
                return false;
            }

            if (!context.UserIdentity.IsAuthenticated)
            {
                //Wrap so we can be authenticated
                context.UserIdentity = new SanteDBClaimsIdentity(context.UserIdentity.Name, true, "LOCAL", (context.UserIdentity as IClaimsIdentity)?.Claims);
            }

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
                var json = _SymmetricProvider.DecryptString(authorizationCode);

                return JsonConvert.DeserializeObject<AuthorizationCode>(json);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}

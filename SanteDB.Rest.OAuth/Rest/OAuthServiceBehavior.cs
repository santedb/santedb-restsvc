/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using SanteDB;
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.OAuth;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Security;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Configuration;
using SanteDB.Rest.OAuth.Model;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Xml;

#pragma warning disable CS0612
namespace SanteDB.Rest.OAuth.Rest
{
    /// <summary>
    /// OAuth2 Access Control Service
    /// </summary>
    /// <remarks>An Access Control Service and Token Service implemented using OAUTH 2.0</remarks>
    [ServiceBehavior(Name = "OAuth2", InstanceMode = ServiceInstanceMode.Singleton)]
    [ExcludeFromCodeCoverage]
    public class OAuthServiceBehavior : IOAuthServiceContract
    {
        private const string AUTHORIZATION_COOKIE_NAME = "_a";

        /// <summary>
        /// Trace Source
        /// </summary>
        protected readonly Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        /// <summary>
        /// Policy Enforcement Service.
        /// </summary>
        protected readonly IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// Configuration for OAuth provider.
        /// </summary>
        protected readonly OAuthConfigurationSection m_configuration;

        /// <summary>
        /// Master secuirity configuration.
        /// </summary>
        protected readonly SecurityConfigurationSection m_masterConfig;

        /// <summary>
        /// Localization service.
        /// </summary>
        protected readonly ILocalizationService m_LocalizationService;

        /// <summary>
        /// Session resolver
        /// </summary>
        protected readonly ISessionTokenResolverService m_SessionResolver;

        /// <summary>
        /// Session Provider
        /// </summary>
        protected readonly ISessionProviderService m_SessionProvider;
        /// <summary>
        /// Session Identity Provider that can authenticate and return a principal for a given session.
        /// </summary>
        protected readonly ISessionIdentityProviderService m_SessionIdentityProvider;
        /// <summary>
        /// Application identity provider.
        /// </summary>
        protected readonly IApplicationIdentityProviderService m_AppIdentityProvider;
        /// <summary>
        /// Device identity provider.
        /// </summary>
        protected readonly IDeviceIdentityProviderService m_DeviceIdentityProvider;
        /// <summary>
        /// JWT Handler to create JWTs with.
        /// </summary>
        protected readonly JsonWebTokenHandler m_JwtHandler;
        /// <summary>
        /// Applet solution manager.
        /// </summary>
        protected readonly IAppletSolutionManagerService _AppletSolutionManager;

        /// <summary>
        /// Applet manager for use in contexts where multiple solutions are not supported (like the dCDR)
        /// </summary>
        protected readonly IAppletManagerService _AppletManager;

        private IAssetProvider _AssetProvider;
        /// <summary>
        /// Symmetric encryption provider.
        /// </summary>
        protected readonly ISymmetricCryptographicProvider _SymmetricProvider;

        readonly IAuditService _AuditService;

        readonly IRoleProviderService _RoleProvider;
        private readonly IDataSigningCertificateManagerService _SigningCertificateManager;



        // XHTML
        private const string XS_HTML = "http://www.w3.org/1999/xhtml";
        /// <summary>
        /// A list of grant type names and corresponding <see cref="ITokenRequestHandler"/> to process the request.
        /// </summary>
        protected readonly Dictionary<string, ITokenRequestHandler> _TokenRequestHandlers;
        private readonly Dictionary<string, Func<OAuthAuthorizeRequestContext, object>> _AuthorizeResponseModeHandlers;

        /// <summary>
        /// Policy enforcement service
        /// </summary>
        public OAuthServiceBehavior()
        {
            _AuditService = ApplicationServiceContext.Current.GetAuditService();
            _AppletManager = ApplicationServiceContext.Current.GetService<IAppletManagerService>();
            m_policyEnforcementService = ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>();
            var configurationManager = ApplicationServiceContext.Current.GetService<IConfigurationManager>();
            m_configuration = configurationManager.GetSection<OAuthConfigurationSection>();
            m_masterConfig = configurationManager.GetSection<SecurityConfigurationSection>();
            m_LocalizationService = ApplicationServiceContext.Current.GetService<ILocalizationService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ILocalizationService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionResolver = ApplicationServiceContext.Current.GetService<ISessionTokenResolverService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionTokenResolverService)} in {nameof(ApplicationServiceContext)}.");
            m_SessionProvider = ApplicationServiceContext.Current.GetService<ISessionProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_AppIdentityProvider = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IApplicationIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            m_DeviceIdentityProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            _SymmetricProvider = ApplicationServiceContext.Current.GetService<ISymmetricCryptographicProvider>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISymmetricCryptographicProvider)} in {nameof(ApplicationServiceContext)}.");
            _RoleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(IRoleProviderService)} in {nameof(ApplicationServiceContext)}.");
            _SigningCertificateManager = ApplicationServiceContext.Current.GetService<IDataSigningCertificateManagerService>();
            //Optimization - try to resolve from the same session provider. 
            m_SessionIdentityProvider = m_SessionProvider as ISessionIdentityProviderService;

            //Fallback and resolve from DI.
            if (null == m_SessionIdentityProvider)
            {
                m_SessionIdentityProvider = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>() ?? throw new ApplicationException($"Cannot find instance of {nameof(ISessionIdentityProviderService)} in {nameof(ApplicationServiceContext)}.");
            }

            m_JwtHandler = new JsonWebTokenHandler();


            if (m_configuration.TokenType != OAuthConstants.BearerTokenType)
            {
                TokenAuthorizationAccessBehavior.SetSessionResolverDelegate(GetSessionFromIdToken);
            }

            //Wire up token request handlers.
            var servicemanager = ApplicationServiceContext.Current.GetService<IServiceManager>();

            var tokenhandlers = servicemanager.CreateInjectedOfAll<ITokenRequestHandler>();

            _TokenRequestHandlers = new Dictionary<string, ITokenRequestHandler>();

            foreach (var handler in tokenhandlers)
            {
                foreach (var granttype in handler.SupportedGrantTypes)
                {
                    if (string.IsNullOrEmpty(granttype))
                    {
                        continue;
                    }

                    try
                    {
                        _TokenRequestHandlers.Add(granttype.Trim().ToLowerInvariant(), handler);
                    }
                    catch (ArgumentException)
                    {
                        m_traceSource.TraceError($"Configuration error. Multiple handlers are configured for the grant type {granttype}. The handler {handler.ServiceName} was not added.");
                    }
                }
            }


            _AppletSolutionManager = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();

            ApplicationServiceContext.Current.Started += Application_Started;

            _AuthorizeResponseModeHandlers = new Dictionary<string, Func<OAuthAuthorizeRequestContext, object>>();
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_Query, RenderQueryResponseMode);
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_Fragment, RenderFragmentResponseMode);
            _AuthorizeResponseModeHandlers.Add(OAuthConstants.ResponseMode_FormPost, RenderFormPostResponseMode);


        }

        private void Application_Started(object sender, EventArgs e)
        {
            //We have to wait for stage 2 otherwise we cannot guarantee that the applets will be loaded.
            if (!string.IsNullOrEmpty(m_configuration.LoginAssetPath))
            {
                m_traceSource.TraceVerbose("Using configuration LoginAssetPath \"{0}\"", m_configuration.LoginAssetPath);
                _AssetProvider = new LocalFolderAssetProvider(m_configuration.LoginAssetPath);
            }
            else if (!string.IsNullOrEmpty(m_configuration.LoginAssetSolution))
            {
                m_traceSource.TraceVerbose("Using configuration asset solution {0}", m_configuration.LoginAssetSolution);

                var applets = _AppletSolutionManager.GetApplets(m_configuration.LoginAssetSolution);

                _AssetProvider = new AppletAssetProvider(applets);
            }
            else if (_AppletSolutionManager != null)
            {
                m_traceSource.TraceVerbose("Using default solution \"santedb.core.sln\"");
                var applets = _AppletSolutionManager.GetApplets("santedb.core.sln");
                _AssetProvider = new AppletAssetProvider(applets);
            }
            else
            {
                m_traceSource.TraceVerbose("No solutions are present. OAuth will search in any loaded applets for login assets.");
                _AssetProvider = new AppletAssetProvider(_AppletManager.Applets);
            }
        }

        #region Helper Methods
        /// <summary>
        /// Try to resolve a device identity from a token request context.
        /// </summary>
        /// <param name="context">The context for the request.</param>
        /// <returns></returns>
        protected bool TryGetDeviceIdentity(OAuthRequestContextBase context)
        {
            if (null == context)
            {
                m_traceSource.TraceInfo("Call to {0} with null context", nameof(TryGetDeviceIdentity));
                return false;
            }

            if (null != context.AuthenticationContext?.Principal?.Identity && context.AuthenticationContext.Principal.Identity is IDeviceIdentity deviceIdentity)
            {
                context.DeviceIdentity = deviceIdentity as IClaimsIdentity;
                context.DevicePrincipal = context.AuthenticationContext.Principal as IClaimsPrincipal;
                m_traceSource.TraceVerbose("Found device identity from {1}: {0}", context.DeviceIdentity, nameof(AuthenticationContext));
                return true;
            }
            else if (context.AuthenticationContext.Principal is IClaimsPrincipal cp && cp.Identities.OfType<IDeviceIdentity>().Any())
            {
                context.DeviceIdentity = cp.Identities.OfType<IDeviceIdentity>().First() as IClaimsIdentity;
                m_traceSource.TraceVerbose("Found device identity in {1} (composite identity): {0}", context.DeviceIdentity, nameof(AuthenticationContext));
                return true;
            }

            m_traceSource.TraceVerbose("No device identity found in request.");
            return false;

        }

        /// <summary>
        /// Try to resolve an application identity from a token request context.
        /// </summary>
        /// <param name="context">The context for the request.</param>
        /// <returns></returns>
        protected bool TryGetApplicationIdentity(OAuthTokenRequestContext context)
        {
            if (null == context)
            {
                m_traceSource.TraceInfo("Call to {0} with null context", nameof(TryGetApplicationIdentity));
                return false;
            }

            if (null != context.ApplicationPrincipal)
            {
                m_traceSource.TraceVerbose("Existing application principal found in the context.");
                return context?.ApplicationPrincipal?.Identity?.IsAuthenticated == true;
            }

            if (null != context.AuthenticationContext?.Principal?.Identity && context.AuthenticationContext.Principal.Identity is IApplicationIdentity applicationIdentity)
            {
                context.ApplicationIdentity = applicationIdentity as IClaimsIdentity;
                m_traceSource.TraceVerbose("Found application identity from {1}: {0}.", context.ApplicationIdentity, nameof(AuthenticationContext));
                return true;
            }
            else if (context.AuthenticationContext?.Principal is IClaimsPrincipal cp && cp.Identities.OfType<IApplicationIdentity>().Any())
            {
                context.ApplicationIdentity = cp.Identities.OfType<IApplicationIdentity>().First() as IClaimsIdentity;
                // Authenticate the client principal using the current principal if the current identity equals the application that's being requested
                if (context.ApplicationIdentity.Name == context.ClientId)
                {
                    m_traceSource.TraceVerbose("Authenticating as {0}  using authenticated principal {1}", context.ClientId, cp.Identity.Name);
                    context.ApplicationPrincipal = m_AppIdentityProvider.Authenticate(context.ClientId, cp) as IClaimsPrincipal;
                }
                m_traceSource.TraceVerbose("Found application identity from {1} (composite identity): {0}.", context.ApplicationIdentity, nameof(AuthenticationContext));
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(context.ClientId) && !string.IsNullOrWhiteSpace(context.ClientSecret))
            {
                m_traceSource.TraceVerbose("Attempting to authenticate application.");
                var principal = m_AppIdentityProvider.Authenticate(context.ClientId, context.ClientSecret);

                if (null != principal && principal.Identity is IApplicationIdentity appidentity)
                {
                    context.ApplicationIdentity = appidentity as IClaimsIdentity;
                    context.ApplicationPrincipal = principal as IClaimsPrincipal;
                    m_traceSource.TraceVerbose("Application authentication successful. Identity: {0}", context.ApplicationIdentity);
                    return true;
                }
                else if (null != principal)
                {
                    m_traceSource.TraceWarning($"Application authentication successful but identity is not {nameof(IApplicationIdentity)}");
                }
                else
                {
                    m_traceSource.TraceInfo($"Application authentication unsuccessful. Client ID: {context.ClientId}");
                }
            }
            else if (!String.IsNullOrEmpty(context.ClientId) && null != context.DevicePrincipal)
            {
                m_traceSource.TraceVerbose("Attempting to authenticate application with device principal.");
                var principal = m_AppIdentityProvider.Authenticate(context.ClientId, context.DevicePrincipal);

                if (null != principal && principal.Identity is IApplicationIdentity appidentity)
                {
                    context.ApplicationIdentity = appidentity as IClaimsIdentity;
                    context.ApplicationPrincipal = principal as IClaimsPrincipal;
                    m_traceSource.TraceVerbose("Application authentication successful. Identity: {0}", context.ApplicationIdentity);
                    return true;
                }
            }

            m_traceSource.TraceVerbose("No application identity found in request.");
            return false;
        }


        /// <summary>
        /// Checks if the grant type that was provided is allowed by this service. The default implementation checks for a TokenRequestHandler for the grant type.
        /// </summary>
        /// <param name="grantType">The incoming grant type.</param>
        /// <returns>True if the grant type is supported, false otherwise.</returns>
        protected bool IsGrantTypePermitted(string grantType)
        {
            if (string.IsNullOrEmpty(grantType))
            {
                return false;
            }

            return _TokenRequestHandlers.ContainsKey(grantType.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="remoteIp"></param>
        /// <returns></returns>
        protected bool TryGetRemoteIp(HttpListenerRequest request, out string remoteIp)
        {
            if (null == request)
            {
                remoteIp = null;
                return false;
            }

            var xforwardedfor = request.Headers["X-Forwarded-For"];

            if (!string.IsNullOrEmpty(xforwardedfor))
            {
                //We need to split this value up. Successive proxies are supposed to append themselves to the end of this value (https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For). 
                var values = xforwardedfor.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                var val = values.FirstOrDefault()?.Trim();

                remoteIp = val;

            }
            else
            {
                remoteIp = request.RemoteEndPoint?.Address?.ToString();
            }
            return !string.IsNullOrEmpty(remoteIp);
        }

        private string EncodeScope(string oid)
        {
            if (oid.StartsWith(PermissionPolicyIdentifiers.UnrestrictedAll))
            {
                return $"ua{oid.Remove(0, PermissionPolicyIdentifiers.UnrestrictedAll.Length)}";
            }

            return oid;
        }

        /// <summary>
        /// Create a descriptor that can be serialized into a JWT or other token format.
        /// </summary>
        protected OAuthRequestContextBase AddTokenDescriptorToContext(OAuthRequestContextBase context)
        {
            var descriptor = new SecurityTokenDescriptor();

            var claimsPrincipal = m_SessionIdentityProvider.Authenticate(context.Session) as IClaimsPrincipal;

            if (null == context.DeviceIdentity)
            {
                context.DeviceIdentity = claimsPrincipal?.Identities?.OfType<IDeviceIdentity>()?.FirstOrDefault() as IClaimsIdentity;
            }

            if (null == context.ApplicationIdentity)
            {
                context.ApplicationIdentity = claimsPrincipal?.Identities?.OfType<IApplicationIdentity>()?.FirstOrDefault() as IClaimsIdentity;
            }

            if (null == context.UserIdentity)
            {
                context.UserIdentity = claimsPrincipal?.Identities?.Where(i => i != context.ApplicationIdentity && i != context.DeviceIdentity)?.FirstOrDefault();
            }


            // System claims
            var claims = new Dictionary<string, object>();

            if (ClaimMapper.Current.TryGetMapper(ClaimMapper.ExternalTokenTypeJwt, out var mappers))
            {
                foreach (var mappedClaim in mappers.SelectMany(o => o.MapToExternalIdentityClaims(claimsPrincipal.Claims)))
                {
                    // We fold in the value to any existing claim value for example for the "extensions" element we want the claim to be:
                    //  "extensions" : {
                    //      "some_value": {
                    //                  ...
                    //      },
                    //      "some_other": {
                    //                  ...
                    //      }
                    //  }
                    // and not 
                    //  "extensions": [
                    //      {
                    //          "some_value": {
                    if (mappedClaim.Value is IDictionary<String, Object> claimValue && claims.TryGetValue(mappedClaim.Key, out var currentClaimValue) && currentClaimValue is IDictionary<String, Object> currentValue)
                    {
                        claimValue.ForEach(kv => currentValue.Add(kv.Key, kv.Value));
                    }
                    else
                    {
                        claims.AddClaim(mappedClaim.Key, mappedClaim.Value);
                    }
                }
            }
            else
            {
                m_traceSource.TraceInfo("No claim mapper found for claim type {0}. This may indicate an issue with the system configuration.", ClaimMapper.ExternalTokenTypeJwt);
            }

            //Name
            claims.Remove(OAuthConstants.ClaimType_Name);
            claims.Remove(OAuthConstants.ClaimType_Actor);

            var primaryidentity = context.GetPrimaryIdentity();
            var useridentity = context.GetUserIdentity();
            var appidentity = context.GetApplicationIdentity();
            var deviceidentity = context.GetDeviceIdentity();

            var rawjtibuilder = new StringBuilder();

            if (null != primaryidentity)
            {
                claims.AddClaim(OAuthConstants.ClaimType_Name, primaryidentity.Name);
                claims.AddClaim(OAuthConstants.ClaimType_Actor, primaryidentity.FindFirst(SanteDBClaimTypes.Actor)?.Value);
                // Remove sub
                claims.Remove(OAuthConstants.ClaimType_Subject);
                claims.AddClaim(OAuthConstants.ClaimType_Subject, primaryidentity.FindFirst(SanteDBClaimTypes.SecurityId)?.Value);
                rawjtibuilder.Append(primaryidentity.Name);
            }

            if (null != useridentity)
            {
                claims.AddClaim(SanteDBClaimTypes.SanteDBUserIdentifierClaim, useridentity.FindFirst(SanteDBClaimTypes.NameIdentifier)?.Value);
                rawjtibuilder.Append(useridentity.Name);
            }

            if (null != appidentity)
            {
                claims.AddClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, appidentity.FindFirst(SanteDBClaimTypes.NameIdentifier)?.Value);
                rawjtibuilder.Append(appidentity.Name);
            }

            if (null != deviceidentity)
            {
                claims.AddClaim(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, deviceidentity.FindFirst(SanteDBClaimTypes.NameIdentifier)?.Value);
                rawjtibuilder.Append(deviceidentity.Name);
            }

            claims.Remove(OAuthConstants.ClaimType_Sid);
            var sessionid = context.GetSessionId();
            claims.AddClaim(OAuthConstants.ClaimType_Sid, sessionid);
            rawjtibuilder.Append(sessionid);
            claims.AddClaim(OAuthConstants.ClaimType_Nonce, context.Nonce);
            rawjtibuilder.Append(context.Nonce);

            rawjtibuilder.Append(DateTimeOffset.UtcNow.ToString("O"));
            rawjtibuilder.Append(context.IncomingRequest.RequestTraceIdentifier);

            if (m_configuration.EncodeScopes && claims.ContainsKey(SanteDBClaimTypes.SanteDBScopeClaim))
            {
                var scope = claims[SanteDBClaimTypes.SanteDBScopeClaim];

                if (scope is string s)
                {
                    claims[SanteDBClaimTypes.SanteDBScopeClaim] = EncodeScope(s);
                }
                else if (scope is List<string> scopes)
                {
                    for (int i = 0; i < scopes.Count; i++)
                    {
                        scopes[i] = EncodeScope(scopes[i]);
                    }
                }
            }

            var halg = System.Security.Cryptography.SHA256.Create();
            var encodedsession = m_SessionResolver.GetEncodedIdToken(context.Session);

            claims.Add(OAuthConstants.ClaimType_AtHash, halg.ComputeHash(encodedsession, 128));
            claims.Add(OAuthConstants.ClaimType_Jti, halg.ComputeHash(rawjtibuilder.ToString()));

            if (null != useridentity && !claims.ContainsKey(OAuthConstants.ClaimType_Role))
            {
                var roles = _RoleProvider.GetAllRoles(useridentity.Name);

                foreach (var role in roles)
                {
                    claims.AddClaim(OAuthConstants.ClaimType_Role, role);
                }
            }

            descriptor.Claims = claims;

            descriptor.NotBefore = context.Session.NotBefore.UtcDateTime;
            descriptor.Expires = context.Session.NotAfter.UtcDateTime;
            descriptor.IssuedAt = descriptor.NotBefore;
            descriptor.Claims.Remove(SanteDBClaimTypes.SecurityId);

            descriptor.CompressionAlgorithm = CompressionAlgorithms.Deflate;

            // Creates signing credentials for the specified application key - the client will be requesting with client_id of the name rather than key
            var appid = claimsPrincipal.Identities.OfType<IApplicationIdentity>().FirstOrDefault()?.Name ?? context.ClientId; // claimsPrincipal?.Claims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.SanteDBApplicationIdentifierClaim)?.Value;
            descriptor.Audience = appid; //Audience should be the client id of the app.

            descriptor.Issuer = m_configuration.IssuerName;



            // Signing credentials for the application
            // TODO: Expose this as a configuration option - which key to use other than default
            descriptor.SigningCredentials = CreateSigningCredentials($"SA.{appid}", m_configuration.JwtSigningKey, "default");

            // Is the default an HMAC256 key?
            if ((null == descriptor.SigningCredentials ||
                descriptor.SigningCredentials.Algorithm == SecurityAlgorithms.HmacSha256Signature) &&
                RestOperationContext.Current.Data.TryGetValue(OAuthConstants.DataKey_SymmetricSecret, out object clientsecret)) // OPENID States we should use the application client secret to sign the result , we can only do this if we actually have a symm_secret set
            {
                var secret = clientsecret is byte[]? (byte[])clientsecret : Encoding.UTF8.GetBytes(clientsecret.ToString());
                while (secret.Length < 16)
                {
                    secret = secret.Concat(secret).ToArray();
                }

                descriptor.SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret) { KeyId = appid }, SecurityAlgorithms.HmacSha256Signature);
            }

            if (null == descriptor.SigningCredentials)
            {
                throw new ApplicationException("No signing key found in configuration");
            }

            context.SecurityTokenDescriptor = descriptor;

            return context;
        }
        /// <summary>
        /// Creates the proper tokens in the context based on the server configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected OAuthRequestContextBase AddTokensToContext(OAuthRequestContextBase context)
        {
            if (null == context)
            {
                return null;
            }

            if (null != context.Session)
            {
                if (null != context.SecurityTokenDescriptor)
                {
                    context.IdToken = m_JwtHandler.CreateToken(context.SecurityTokenDescriptor);
                }

                context.ExpiresIn = context.Session.NotAfter.Subtract(DateTimeOffset.UtcNow);

                context.TokenType = m_configuration.TokenType;

                if (context.TokenType == OAuthConstants.BearerTokenType)
                {
                    context.AccessToken = m_SessionResolver.GetEncodedIdToken(context.Session);
                }
                else
                {
                    context.AccessToken = context.IdToken;
                }
            }

            return context;
        }

        /// <summary>
        /// Alternate resolution method for <see cref="TokenAuthorizationAccessBehavior"/> when the token type is not bearer.
        /// </summary>
        /// <param name="idToken">The JWT as a string to extract a session from.</param>
        /// <returns>The related <see cref="ISession"/> session for the idtoken.</returns>
        protected virtual ISession GetSessionFromIdToken(string idToken)
        {
            var result = m_JwtHandler.ValidateToken(idToken, GetTokenValidationParameters());

            if (result?.IsValid != true)
            {
                return null;
            }

            var claims = result.Claims.ToList();

            var sessionidstr = claims.FirstOrDefault(clm => clm.Key == OAuthConstants.ClaimType_Sid).Value?.ToString();

            if (!Guid.TryParse(sessionidstr, out var sessionid))
            {
                return null;
            }

            var session = m_SessionProvider.Get(sessionid.ToByteArray());

            return session;
        }


        /// <summary>
        /// Gets a <see cref="TokenValidationParameters"/> object to validate tokens issued by this service.
        /// </summary>
        /// <returns>A <see cref="TokenValidationParameters"/> object configured for the current issuer's configuration.</returns>
        protected virtual TokenValidationParameters GetTokenValidationParameters()
        {
            var tvp = new TokenValidationParameters();

            var jwks = GetJsonWebKeySet();
            jwks.SkipUnresolvedJsonWebKeys = false; //Fix for HS256 Keys.
            tvp.IssuerSigningKeys = jwks.GetSigningKeys();
            tvp.ValidIssuer = m_configuration.IssuerName;
            tvp.ValidateIssuer = true;
            tvp.ValidateIssuerSigningKey = true;
            tvp.ValidateLifetime = true;
            tvp.ValidateAudience = false;
            tvp.TryAllIssuerSigningKeys = true;
            tvp.ClockSkew = TimeSpan.FromSeconds(5); //Should be minimal since we're the ones issuing.

            tvp.NameClaimType = GetNameClaimType();

            return tvp;
        }

        /// <summary>
        /// Retrieves the claim type that is used for name validation in the <see cref="TokenValidationParameters"/>.
        /// </summary>
        /// <returns>A claim type for the name claim.</returns>
        protected virtual string GetNameClaimType()
        {
            if (ClaimMapper.Current.TryGetMapper(ClaimMapper.ExternalTokenTypeJwt, out var mappers))
            {
                foreach (var mapper in mappers)
                {
                    var mapped = mapper.MapToExternalClaimType(SanteDBClaimTypes.DefaultNameClaimType);

                    if (mapped != SanteDBClaimTypes.DefaultNameClaimType)
                    {
                        return mapped;
                    }
                }
            }

            return OAuthConstants.ClaimType_Name;
        }

        /// <summary>
        /// Establishes a session for a daemon application and optional device identity. No user is associated with the session.
        /// </summary>
        /// <param name="clientPrincipal">The application which the session will be created for.</param>
        /// <param name="devicePrincipal">An optional device identity associated with the session.</param>
        /// <param name="scopes">Scopes that the session is granted</param>
        /// <param name="additionalClaims">Additional claims to establish with the session.</param>
        /// <returns>A session object that can be used to perform operations with.</returns>
        /// <exception cref="ArgumentException">The <paramref name="clientPrincipal"/> is not an <see cref="IApplicationIdentity"/>.</exception>
        protected ISession EstablishClientSession(IPrincipal clientPrincipal, IPrincipal devicePrincipal, List<string> scopes, IEnumerable<IClaim> additionalClaims)
        {
            SanteDBClaimsPrincipal claimsPrincipal = null;

            if (!(clientPrincipal.Identity is IApplicationIdentity))
            {
                throw new ArgumentException("Client Principal must be an instance of IApplicationIdentity.", nameof(clientPrincipal));
            }

            if (clientPrincipal is IClaimsPrincipal client)
            {
                claimsPrincipal = new SanteDBClaimsPrincipal(client.Identities);
            }
            else
            {
                claimsPrincipal = new SanteDBClaimsPrincipal(clientPrincipal.Identity);
            }

            if (devicePrincipal is IClaimsPrincipal && !claimsPrincipal.Identities.OfType<IDeviceIdentity>().Any(o => o.Name == devicePrincipal.Identity.Name))
            {
                claimsPrincipal.AddIdentity(devicePrincipal.Identity as IClaimsIdentity);
            }

            _ = TryGetRemoteIp(RestOperationContext.Current.IncomingRequest, out var remoteIp);

            // Establish the session

            string purposeOfUse = additionalClaims?.GetPurposeOfUse();
            bool isOverride = additionalClaims.HasOverrideClaim() || scopes.HasOverrideScope();
            var session = m_SessionProvider.Establish(claimsPrincipal, remoteIp, isOverride, purposeOfUse, scopes?.ToArray(), additionalClaims.GetLanguage());

            _AuditService.Audit().ForSessionStart(session, claimsPrincipal, true).Send();

            return session;
        }

        /// <summary>
        /// Create a token response
        /// </summary>
        protected ISession EstablishUserSession(IPrincipal primaryPrincipal, IClaimsIdentity clientIdentity, IClaimsIdentity deviceIdentity, List<string> scopes, IEnumerable<IClaim> additionalClaims)
        {
            IClaimsPrincipal claimsPrincipal = null;

            // JF - Special case - if the upstream principal is already a token principal - there is no need to establish a new session or principal since the 
            //      issuer of the token has already done this and we just need to store it and reliably load it from our session provider - by forwarding the 
            //      ITokenPrincipal down (rather than creating a new principal) we allow the downstream session providers to store access tokens, expiration
            //      and other information on the original principal
            if (primaryPrincipal is IClaimsPrincipal cp && primaryPrincipal is ITokenPrincipal tokenPrincipal)
            {
                claimsPrincipal = cp;
            }
            else
            {
                // JF - We may need to add other identities to the principal which some  principals might not  like
                if (primaryPrincipal is IClaimsPrincipal userprincipal)
                {
                    claimsPrincipal = new SanteDBClaimsPrincipal(userprincipal.Identities);
                }
                else
                {
                    claimsPrincipal = new SanteDBClaimsPrincipal(primaryPrincipal.Identity);
                }

                if (clientIdentity is IApplicationIdentity && !claimsPrincipal.Identities.OfType<IApplicationIdentity>().Any(o => o.Name == clientIdentity.Name))
                {
                    claimsPrincipal.AddIdentity(clientIdentity);
                }

                if (deviceIdentity is IDeviceIdentity && !claimsPrincipal.Identities.OfType<IDeviceIdentity>().Any(o => o.Name == deviceIdentity.Name))
                {
                    claimsPrincipal.AddIdentity(deviceIdentity);
                }
            }

            _ = TryGetRemoteIp(RestOperationContext.Current.IncomingRequest, out var remoteIp);

            // Establish the session

            string purposeOfUse = additionalClaims.GetPurposeOfUse();
            bool isOverride = additionalClaims.HasOverrideClaim() || scopes.HasOverrideScope();

            var session = m_SessionProvider.Establish(claimsPrincipal, remoteIp, isOverride, purposeOfUse, scopes?.ToArray(), additionalClaims.GetLanguage());

            _AuditService.Audit().ForSessionStart(session, claimsPrincipal, true).Send();

            return session;
        }

        /// <summary>
        /// Create error condition
        /// </summary>
        private OAuthError CreateErrorResponse(OAuthErrorType errorType, string message, string detail = null, string state = null, IDictionary<String, Object> errorData = null)
        {
            m_traceSource.TraceInfo("Returning OAuthError: Type: {0} , Message: {1}, State: {2}", errorType.ToString(), message, state ?? "(null)");
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.BadRequest;
            return new OAuthError()
            {
                Error = errorType,
                ErrorDescription = message,
                ErrorDetail = detail,
                State = state,
                ErrorData = errorData
            };
        }

        /// <summary>
        /// Render the specified asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="context">The request context.</param>
        /// <returns></returns>
        private Stream RenderInternal(string assetPath, OAuthAuthorizeRequestContext context)
        {
            var locale = context.IncomingRequest.QueryString["ui_locale"];

            Stream content = null;
            string mimetype = null;

            var bindingparameters = new Dictionary<string, string>();
            bindingparameters.Add("client_id", context.ClientId);
            bindingparameters.Add("redirect_uri", context.RedirectUri);
            bindingparameters.Add("response_type", context.ResponseType);
            bindingparameters.Add("response_mode", context.ResponseMode);
            bindingparameters.Add("state", context.State);
            bindingparameters.Add("scope", context.Scope);
            bindingparameters.Add("nonce", context.Nonce);
            bindingparameters.Add("login_hint", context.LoginHint);
            bindingparameters.Add("username", context.Username);
            bindingparameters.Add("activity_id", context.ActivityId.ToString());
            // bindingparameters.Add("password", context.FormFields?["password"]); //We don't send the password back on an invalid login.

            bindingparameters.Add("error_message", context.ErrorMessage);

            var restcontext = RestOperationContext.Current;

            restcontext.OutgoingResponse.Headers.Add("X-Frame-Options", "SAMEORIGIN");

            EventHandler handler = (s, e) =>
            {
                content?.Dispose();
            };

            restcontext.Disposed += handler;

            try
            {
                (content, mimetype) = _AssetProvider.GetAsset(assetPath, locale, bindingparameters);

                if (null != content)
                {
                    RestOperationContext.Current.OutgoingResponse.ContentType = mimetype;
                    return content;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.StatusDescription = "NO CONTENT";
                    return Stream.Null;
                }
            }
            catch (FileNotFoundException)
            {
                m_traceSource.TraceVerbose("Asset not found: \"{0}\"", assetPath);
                RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                RestOperationContext.Current.OutgoingResponse.StatusDescription = "NOT FOUND";
                return Stream.Null;
            }
            finally
            {
                restcontext.Disposed -= handler;
            }
        }

        private static SecurityKey GetSecurityKeyForHs256Configuration(SecuritySignatureConfiguration configuration)
        {
            byte[] secret = configuration.GetSecret().ToArray();
            while (secret.Length < 16) //TODO: Why are we doing this?
            {
                secret = secret.Concat(secret).ToArray();
            }

            var key = new SymmetricSecurityKey(secret);

            if (!string.IsNullOrEmpty(configuration.KeyName))
            {
                key.KeyId = configuration.KeyName;
            }
            else
            {
                key.KeyId = "0"; //Predefined default KID
            }

            return key;
        }

        /// <summary>
        /// Create signing credentials
        /// </summary>
        /// <param name="keyNames">One or more key names to search (in-order) for a signature for.</param>
        private SigningCredentials CreateSigningCredentials(params string[] keyNames)
        {
            if (null == keyNames || keyNames.Length == 0)
            {
                throw new ArgumentNullException(nameof(keyNames), "Key names are required.");
            }

            SecuritySignatureConfiguration configuration = null;

            foreach (var keyname in keyNames)
            {
                configuration = m_masterConfig.Signatures.FirstOrDefault(s => s.KeyName == keyname);

                if (null != configuration)
                {
                    break;
                }
            }

            if (null == configuration) //No configuration provided, return a null credential back.
            {
                return null;
            }

            // Signing credentials
            switch (configuration.Algorithm)
            {
                case SignatureAlgorithm.RS256:
                case SignatureAlgorithm.RS512:
                    var cert = configuration.Certificate;
                    if (cert == null)
                    {
                        throw new SecurityException("Cannot find certificate to sign data!");
                    }

                    // Signature algorithm
                    string signingAlgorithm = SecurityAlgorithms.RsaSha256;
                    if (configuration.Algorithm == SignatureAlgorithm.RS512)
                    {
                        signingAlgorithm = SecurityAlgorithms.RsaSha512;
                    }
                    return new X509SigningCredentials(cert, signingAlgorithm);
                case SignatureAlgorithm.HS256:
                    var key = GetSecurityKeyForHs256Configuration(configuration);
                    return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                default:
                    throw new SecurityException("Invalid signing configuration");
            }
        }
        #endregion

        #region Token Endpoint
        /// <summary>
        /// OAuth token request
        /// </summary>
        public virtual object Token(NameValueCollection formFields)
        {
            m_traceSource.TraceVerbose("Processing token request.");

            if (null == formFields || formFields.Count == 0)
            {
                m_traceSource.TraceVerbose("Empty request received. Returning error.");
                return CreateErrorResponse(OAuthErrorType.invalid_request, "request is empty.");
            }

            var context = new OAuthTokenRequestContext(RestOperationContext.Current, formFields);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            context.Nonce = formFields[OAuthConstants.FormField_Nonce];

            if (!IsGrantTypePermitted(context.GrantType))
            {
                m_traceSource.TraceInfo("Request has unsupported grant type {0}", context.GrantType);
                return CreateErrorResponse(OAuthErrorType.unsupported_grant_type, $"unsupported grant type {context.GrantType}");
            }

            //HACK: Remove this when we figure out how to complete the refactor.
            if (!string.IsNullOrEmpty(context.ClientSecret))
            {
                m_traceSource.TraceVerbose("Adding symmetric key override from client secret in request.");
                context.OperationContext.Data.Add(OAuthConstants.DataKey_SymmetricSecret, context.ClientSecret);
            }

            _ = TryGetDeviceIdentity(context);
            _ = TryGetApplicationIdentity(context);


            var clientClaims = context.IncomingRequest.Headers.ExtractClientClaims().ToList();
            // Set the language claim?
            if (!string.IsNullOrEmpty(formFields[OAuthConstants.FormField_UILocales]) &&
                !clientClaims.Any(o => o.Type == SanteDBClaimTypes.Language))
            {
                clientClaims.Add(new SanteDBClaim(SanteDBClaimTypes.Language, formFields[OAuthConstants.FormField_UILocales]));
            }

            context.AdditionalClaims = clientClaims;

            // Add demanded sopes from the request
            context.Scopes = formFields[OAuthConstants.FormField_Scope]?.Split(' ').ToList();

            var handler = _TokenRequestHandlers[context.GrantType];

            if (null == handler) //How did this happen?
            {
                m_traceSource.TraceWarning("Found null handler for grant type {0}.", context.GrantType);
                return CreateErrorResponse(OAuthErrorType.unsupported_grant_type, $"unsupported grant type {context.GrantType}");
            }

            m_traceSource.TraceVerbose("Executing token request handler.");
            try
            {
                bool success = handler.HandleRequest(context);

                if (!success)
                {
                    m_traceSource.TraceVerbose("Handler returned error. Type: {1}, Message: {2}", context.ErrorType ?? OAuthErrorType.unspecified_error, context.ErrorMessage);
                    return CreateErrorResponse(context.ErrorType ?? OAuthErrorType.unspecified_error, context.ErrorMessage ?? "unspecified error", context.ErrorDetail);
                }

                if (null == context.Session) //If the session is null, the handler is delegating session initialization back to us.
                {
                    m_traceSource.TraceVerbose($"Establishing session in {nameof(OAuthServiceBehavior)}. This is expected when the handler does not initialize the session.");

                    if (null != context.UserPrincipal)
                    {
                        context.Session = EstablishUserSession(context.UserPrincipal, context.ApplicationIdentity, context.DeviceIdentity, context.Scopes, context.AdditionalClaims);
                    }
                    else if (null != context.ApplicationPrincipal)
                    {
                        context.Session = EstablishClientSession(context.ApplicationPrincipal, context.DevicePrincipal, context.Scopes, context.AdditionalClaims);
                    }
                    else
                    {
                        throw new NotSupportedException("Neither a user principal or application principal was returned. Authentication cannot occur.");
                    }

                    _AuditService.Audit().ForSessionStart(context.Session, context.UserPrincipal ?? context.ApplicationPrincipal, context.Session != null).Send();

                    if (null == context.Session)
                    {
                        m_traceSource.TraceInfo("Error establishing session and handler indicated success.");
                        return CreateErrorResponse(OAuthErrorType.unspecified_error, "error establishing session");
                    }
                }

                m_traceSource.TraceInfo("Token request complete, creating response.");

                AddTokenDescriptorToContext(context);

                AddTokensToContext(context);

                var response = CreateTokenResponse(context);

                BeforeSendTokenResponse(context, response);

                return response;
            }
            // Error establishing the session
            catch (SecuritySessionException sessionException) when (sessionException.Type == SessionExceptionType.MissingRequiredClaim)
            {
                this.m_traceSource.TraceWarning("Attempted to login but policy requires a claim be provided");

                return this.CreateErrorResponse(OAuthErrorType.missing_claim, sessionException.Message, errorData: sessionException.Data.Keys.OfType<String>().ToDictionary(o=>o, o=> sessionException.Data[o]));
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                m_traceSource.TraceError("Unhandled exception from token request handler: {0}", ex.ToString());
                // Allow the error behavior to create an appropriate error
                throw; //  return CreateErrorResponse(OAuthErrorType.unspecified_error, ex.Message);
            }
        }

        /// <summary>
        /// Optional override method that is executed just before a token response is sent. Allows a derived class to override the response.
        /// </summary>
        /// <param name="context">The request context</param>
        /// <param name="response">The response object that is sent back to the clinet.</param>
        protected virtual void BeforeSendTokenResponse(OAuthTokenRequestContext context, Model.OAuthTokenResponse response)
        {

        }

        /// <summary>
        /// Create a token response.
        /// </summary>
        /// <param name="context">The <see cref="OAuthRequestContextBase"/> that is used to create the response.</param>
        /// <returns>A completed response that can be provided to the caller.</returns>
        protected Model.OAuthTokenResponse CreateTokenResponse(OAuthTokenRequestContext context)
        {
            if (null == context)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (null != context.OutgoingResponse)
            {
                context.OutgoingResponse.ContentType = "application/json";
            }

            var response = new Model.OAuthTokenResponse();

            response.IdToken = context.IdToken;
            response.ExpiresIn = unchecked((int)Math.Floor(context.ExpiresIn.TotalSeconds));
            response.TokenType = m_configuration.TokenType;
            response.AccessToken = context.AccessToken;

            response.Nonce = context.Nonce;

            if (null != context.Session.RefreshToken)
            {
                response.RefreshToken = m_SessionResolver.GetEncodedRefreshToken(context.Session);
            }

            return response;
        }

        #endregion

        #region Session Endpoint
        /// <summary>
        /// Get the specified session information
        /// </summary>
        public virtual object Session()
        {
            // If the user calls this with no session - we just return no session
            if (String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.Headers["Authorization"]))
            {
                return null; // There is no way to try to load the session
            }

            if (RestOperationContext.Current.Data.TryGetValue(TokenAuthorizationAccessBehavior.RestPropertyNameSession, out var sessobj))
            {
                if (sessobj is ISession session)
                {
                    var context = new OAuthSessionRequestContext(RestOperationContext.Current);
                    context.Session = session;

                    AddTokenDescriptorToContext(context);
                    AddTokensToContext(context);

                    return CreateSessionResponse(context);

                }
            }

            return new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = "No Such Session"
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected OAuthSessionResponse CreateSessionResponse(OAuthSessionRequestContext context)
        {
            if (null == context)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (null != context.OutgoingResponse)
            {
                context.OutgoingResponse.ContentType = "application/json";
            }

            var response = new OAuthSessionResponse();

            response.IdToken = context.IdToken;
            response.ExpiresIn = unchecked((int)Math.Floor(context.ExpiresIn.TotalSeconds));
            response.TokenType = context.TokenType;
            response.AccessToken = context.AccessToken;

            return response;
        }
        #endregion

        #region Authorize Endpoint
        /// <summary>
        /// HTTP GET Authorization Endpoint.
        /// </summary>
        /// <returns></returns>
        public object Authorize()
        {
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            return AuthorizeInternal(context);
        }

        /// <summary>
        /// HTTP POST Authorization endpoint.
        /// </summary>
        /// <param name="formFields"></param>
        /// <returns></returns>
        public object Authorize_Post(NameValueCollection formFields)
        {
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current, formFields);
            context.AuthenticationContext = AuthenticationContext.Current;
            context.Configuration = m_configuration;

            return AuthorizeInternal(context);
        }

        /// <summary>
        /// Internal method for <see cref="Authorize" /> and <see cref="Authorize_Post(NameValueCollection)"/>.
        /// </summary>
        /// <param name="context">Request context</param>
        /// <returns>An object to respond to the caller with.</returns>
        private object AuthorizeInternal(OAuthAuthorizeRequestContext context)
        {
            if (!IsAuthorizeRequestValid(context, out var error))
            {
                return error ?? CreateErrorResponse(OAuthErrorType.unspecified_error, "invalid request", context.State);
            }

            _ = TryGetDeviceIdentity(context);

            if (null != context.DevicePrincipal)
            {
                m_policyEnforcementService?.Demand(OAuthConstants.OAuthCodeFlowPolicy, context.DevicePrincipal);
            }

            context.ApplicationIdentity = m_AppIdentityProvider.GetIdentity(context.ClientId) as IClaimsIdentity;

            if (null == context.ApplicationIdentity || context.ApplicationIdentity.Claims.FirstOrDefault(c => c.Type == SanteDBClaimTypes.SecurityId)?.Value == AuthenticationContext.SystemApplicationSid)
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_client, $"unrecognized client: {context.ClientId}", context.State);
                return false;
            }

            if (!string.IsNullOrEmpty(context.Username))
            {
                try
                {
                    var identityprovider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

                    context.UserPrincipal = identityprovider.Authenticate(context.Username, context.Password) as IClaimsPrincipal;
                    context.UserIdentity = context.UserPrincipal?.Identities?.FirstOrDefault();

                    if (null != context.UserPrincipal)
                    {
                        CreateAuthorizationCode(context);

                        SetAuthorizationCookie(context);

                        var responsehandler = _AuthorizeResponseModeHandlers[context.ResponseMode];

                        return responsehandler(context);
                    }
                }
                catch (AuthenticationException aex)
                {
                    m_traceSource.TraceInfo("Authentication exception in oauth authorize. {0}", aex.ToString());
                    context.ErrorMessage = aex.Message;
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    if (ex.InnerException is AuthenticationException aex)
                    {
                        m_traceSource.TraceInfo("Authentication exception in oauth authorize. {0}", aex.ToString());
                        context.ErrorMessage = aex.Message;
                    }
                    else
                    {
                        m_traceSource.TraceWarning("Exception in oauth authorize. {0}", ex.ToString());
                        context.ErrorMessage = ex.Message;
                    }
                }
            }


            return RenderInternal(null, context);
        }

        private Model.AuthorizationCookie GetAuthorizationCookie(OAuthRequestContextBase context)
        {
            if (null == context || null == context.IncomingRequest)
            {
                return null;
            }

            var cookie = context.IncomingRequest.Cookies?[AUTHORIZATION_COOKIE_NAME];

            if (!string.IsNullOrEmpty(cookie?.Value) && !(cookie?.Expired == true))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Model.AuthorizationCookie>(_SymmetricProvider.Decrypt(cookie.Value));
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    m_traceSource.TraceWarning("Received invalid authorization cookie value in request. This could be an attempt to circumvent security.");
                }
            }

            return null;
        }

        private void SetAuthorizationCookie(OAuthRequestContextBase context)
        {
            if (null == context?.UserPrincipal || null == context.IncomingRequest || null == context.OutgoingResponse || context.UserPrincipal?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var cookie = context.IncomingRequest.Cookies?[AUTHORIZATION_COOKIE_NAME];

            if (null == cookie)
            {
                cookie = new Cookie(AUTHORIZATION_COOKIE_NAME, null);
            }

            cookie.Expires = GetCookieExpirationDate();

            Model.AuthorizationCookie cookievalue = null;

            if (!string.IsNullOrEmpty(cookie.Value))
            {
                try
                {
                    cookievalue = JsonConvert.DeserializeObject<Model.AuthorizationCookie>(_SymmetricProvider.Decrypt(cookie.Value));
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    m_traceSource.TraceWarning("Received invalid authorization cookie value in request. This could be an attempt to circumvent security.");
                }
            }

            if (null == cookievalue)
            {
                cookievalue = new AuthorizationCookie();
            }

            if (null == cookievalue.Users)
            {
                cookievalue.Users = new List<string>();
            }

            var user = context.UserPrincipal.Identity.Name;

            if (!cookievalue.Users.Contains(user))
            {
                cookievalue.Users.Add(user);
                cookievalue.CreatedAt = DateTimeOffset.UtcNow;
            }

            cookie.Value = _SymmetricProvider.Encrypt(JsonConvert.SerializeObject(cookievalue));

            context.OutgoingResponse.SetCookie(cookie);
        }

        private DateTime GetCookieExpirationDate()
        {
            var duration = m_masterConfig.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.AuthenticationCookieValidityLength, TimeSpan.FromHours(1));

            return DateTime.Now.Add(duration);
        }



        /// <summary>
        /// Makes an authorization code and adds it to the context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private OAuthAuthorizeRequestContext CreateAuthorizationCode(OAuthAuthorizeRequestContext context)
        {
            var authcode = new AuthorizationCode();
            authcode.iat = DateTimeOffset.UtcNow;
            authcode.scp = context.Scope;
            authcode.usr = context.UserPrincipal.GetClaimValue(SanteDBClaimTypes.SecurityId);
            authcode.app = context.ApplicationIdentity?.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.SecurityId)?.Value;
            authcode.dev = context.DeviceIdentity?.Claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.SecurityId)?.Value;
            authcode.nonce = context.Nonce;

            var codejson = JsonConvert.SerializeObject(authcode);

            context.Code = _SymmetricProvider.Encrypt(codejson);

            return context;
        }

        /// <summary>
        /// Validate an <see cref="OAuthAuthorizeRequestContext"/> context contains a valid request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool IsAuthorizeRequestValid(OAuthAuthorizeRequestContext context, out OAuthError error)
        {
            if (null == context)
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "empty request", null);
                return false;
            }

            if (string.IsNullOrEmpty(context.ClientId))
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "missing client_id", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseType))
            {
                error = CreateErrorResponse(OAuthErrorType.invalid_request, "missing response_type", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseType))
            {
                context.ResponseType = "code"; //Default
            }

            if (context.ResponseType != "code")
            {
                error = CreateErrorResponse(OAuthErrorType.unsupported_response_type, $"response_type '{context.ResponseType}' not supported", context.State);
                return false;
            }

            if (string.IsNullOrEmpty(context.ResponseMode))
            {
                switch (context.ResponseType)
                {
                    case "code":
                        context.ResponseMode = "query";
                        break;
                    case "token":
                        context.ResponseMode = "fragment";
                        break;
                    default:
                        break;
                }
            }

            if (!_AuthorizeResponseModeHandlers.ContainsKey(context.ResponseMode))
            {
                error = CreateErrorResponse(OAuthErrorType.unsupported_response_mode, $"response_mode '{context.ResponseMode}' is not supported", context.State);
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Render an authorization response in the form [redirect_uri]?code=XXX&amp;state=YYY
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Stream RenderQueryResponseMode(OAuthAuthorizeRequestContext context)
        {
            context.OutgoingResponse.StatusCode = 302;
            context.OutgoingResponse.StatusDescription = "FOUND";
            context.OutgoingResponse.RedirectLocation = $"{context.RedirectUri}?code={context.Code}&state={context.State}";
            return null;
        }

        /// <summary>
        /// Render an authorization response in the form [redirect_uri]#code=XXX&amp;state=YYY
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Stream RenderFragmentResponseMode(OAuthAuthorizeRequestContext context)
        {
            context.OutgoingResponse.StatusCode = 302;
            context.OutgoingResponse.StatusDescription = "FOUND";
            context.OutgoingResponse.RedirectLocation = $"{context.RedirectUri}#code={context.Code}&state={context.State}";
            return null;
        }

        /// <summary>
        /// Render a redirect oauth post which will post to redirect uri a set of form values with application/x-www-form-urlencoded encoding.
        /// </summary>
        private Stream RenderFormPostResponseMode(OAuthAuthorizeRequestContext context)
        {
            var responsedata = new Dictionary<string, string>();
            responsedata.Add("state", context.State);
            responsedata.Add("code", context.Code);

            var ms = new MemoryStream();
            RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { CloseOutput = false, OmitXmlDeclaration = true }))
            {
                xw.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
                xw.WriteStartElement("html", XS_HTML);
                xw.WriteStartElement("head");
                xw.WriteStartElement("title");
                xw.WriteString("Submit This Form");
                xw.WriteEndElement(); // title
                xw.WriteEndElement(); // head

                xw.WriteStartElement("body", XS_HTML);
                xw.WriteAttributeString("onload", "javascript:document.forms[0].submit()");

                xw.WriteStartElement("form", XS_HTML);
                xw.WriteAttributeString("method", "POST");
                xw.WriteAttributeString("action", context.RedirectUri);

                // Emit data
                foreach (var itm in responsedata)
                {
                    xw.WriteStartElement("input");
                    xw.WriteAttributeString("type", "hidden");
                    xw.WriteAttributeString("name", itm.Key);
                    xw.WriteAttributeString("value", itm.Value);
                    xw.WriteEndElement(); // input
                }
                xw.WriteStartElement("button", XS_HTML);
                xw.WriteAttributeString("type", "submit");
                xw.WriteString("Complete Authentication");
                xw.WriteEndElement();
                xw.WriteEndElement(); // form

                xw.WriteEndElement(); // body
                xw.WriteEndElement(); // html
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        #endregion

        #region Content Endpoint
        /// <summary>
        /// Render the specified login asset.
        /// </summary>
        /// <returns>A stream of the rendered login asset</returns>
        public Stream Content(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                RestOperationContext.Current.OutgoingResponse.StatusDescription = "NOT FOUND";
                return null;
            }
            var context = new OAuthAuthorizeRequestContext(RestOperationContext.Current);
            return RenderInternal(assetPath, context);
        }
        #endregion

        #region Ping Endpoint
        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
        }
        #endregion

        #region Discovery Endpoint
        /// <summary>
        /// Gets the discovery object
        /// </summary>
        public OpenIdConnectDiscoveryDocument Discovery()
        {
            try
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
                var authDiscovery = ApplicationServiceContext.Current.GetService<OAuthMessageHandler>() as IApiEndpointProvider;
                var securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();
                var retVal = new OpenIdConnectDiscoveryDocument();

                // mex configuration
                var mexConfig = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Common.Configuration.RestConfigurationSection>();
                string boundHostPort = $"{RestOperationContext.Current.IncomingRequest.Url.Scheme}://{RestOperationContext.Current.IncomingRequest.Url.Host}:{RestOperationContext.Current.IncomingRequest.Url.Port}";
                if (!string.IsNullOrEmpty(mexConfig.ExternalHostPort))
                {
                    var tUrl = new Uri(mexConfig.ExternalHostPort);
                    boundHostPort = $"{tUrl.Scheme}://{tUrl.Host}:{tUrl.Port}";
                }
                boundHostPort = $"{boundHostPort}{new Uri(authDiscovery.Url.First()).AbsolutePath}";

                // Now get the settings
                retVal.Issuer = m_configuration.IssuerName;
                retVal.TokenEndpoint = $"{boundHostPort}/oauth2_token";
                retVal.AuthorizationEndpoint = $"{boundHostPort}/authorize";
                retVal.UserInfoEndpoint = $"{boundHostPort}/userinfo";
                retVal.GrantTypesSupported = _TokenRequestHandlers.Keys.ToList();
                retVal.IdTokenSigning = securityConfiguration.Signatures.Select(o => o.Algorithm).Distinct().Select(o => o.ToString()).ToList();
                retVal.ResponseTypesSupported = new List<string>() { "code" };
                retVal.ScopesSupported = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicies().Select(o => o.Oid).ToList();
                retVal.SigningKeyEndpoint = $"{boundHostPort}/jwks";
                retVal.SubjectTypesSupported = new List<string>() { "public" };
                retVal.SignoutEndpoint = $"{boundHostPort}/signout";
                return retVal;
            }
            catch (Exception e)
            {
                m_traceSource.TraceError("Error generating OpenID Metadata: {0}", e);
                throw new Exception("Error generating OpenID Metadata", e);
            }
        }
        #endregion

        #region UserInfo Endpoint
        /// <summary>
        /// Get the specified session information
        /// </summary>
        public object UserInfo()
        {
            new TokenAuthorizationAccessBehavior().Apply(new RestRequestMessage(RestOperationContext.Current.IncomingRequest));

            if (RestOperationContext.Current.Data.TryGetValue(TokenAuthorizationAccessBehavior.RestPropertyNameSession, out var sessobj))
            {
                if (sessobj is ISession session)
                {
                    var principal = m_SessionIdentityProvider.Authenticate(session) as IClaimsPrincipal;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
                    var claims = new Dictionary<string, object>();

                    foreach (var claim in principal.Claims)
                    {
                        if (null != claim?.Value && !claims.ContainsKey(claim.Type))
                        {
                            claims.Add(claim.Type, claim.Value);
                        }
                    }

                    return claims;
                }
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json";
            return new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = "No Such Session"
            };
        }
        #endregion

        #region JWKS Endpoint
        /// <summary>
        /// Gets the keys associated with this service.
        /// </summary>
        /// <returns></returns>
        public virtual object JsonWebKeySet()
        {
            return new Model.Jwks.KeySet(GetJsonWebKeySet());
        }

        private JsonWebKeySet GetJsonWebKeySet()
        {
            var keyset = new JsonWebKeySet();

            keyset.SkipUnresolvedJsonWebKeys = true;

            foreach (var signkey in m_masterConfig.Signatures)
            {
                if (null == signkey)
                {
                    continue;
                }

                JsonWebKey jwk = null;

                switch (signkey.Algorithm)
                {
                    case SignatureAlgorithm.RS256:
                    case SignatureAlgorithm.RS512:

                        if (null == signkey.Certificate)
                        {
                            continue;
                        }

                        var x509key = new X509SecurityKey(signkey.Certificate);
                        jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(x509key);

                        break;
                    case SignatureAlgorithm.HS256:

                        var secret = signkey.GetSecret().ToArray();

                        if (null == secret)
                        {
                            continue;
                        }


                        while (secret.Length < 16) //TODO: Why are we doing this?
                        {
                            secret = secret.Concat(secret).ToArray();
                        }

                        var hmackey = new SymmetricSecurityKey(secret);

                        if (!string.IsNullOrEmpty(signkey.KeyName))
                        {
                            hmackey.KeyId = signkey.KeyName;
                        }
                        else
                        {
                            hmackey.KeyId = "0"; //Predefined default KID
                        }

                        jwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmackey);

                        break;
                    default:
                        break;
                }

                if (null != jwk && !keyset.Keys.Any(k => k.KeyId == jwk?.KeyId))
                {
                    keyset.Keys.Add(jwk);
                }
            }

            // Include all for SYSTEM
            foreach(var cert in this._SigningCertificateManager.GetSigningCertificates(AuthenticationContext.SystemPrincipal.Identity))
            {
                if(!keyset.Keys.Any(k=>cert.GetCertHash().HexEncode().Equals(k.KeyId, StringComparison.OrdinalIgnoreCase)))
                {
                    keyset.Keys.Add(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(cert)));
                }
            }

            return keyset;
        }
        #endregion

        #region Signout Endpoint
        /// <summary>
        /// Process a signout request flow.
        /// </summary>
        /// <param name="form">The form parameters for the signout request.</param>
        /// <returns>Null. The response should be a redirect to the provided uri or to the default URI.</returns>
        [return: MessageFormat(MessageFormatType.Json)]
        public virtual object Signout(NameValueCollection form)
        {
            if (null == form)
            {
                return null;
            }

            var context = new OAuthSignoutRequestContext(RestOperationContext.Current, form);
            context.AuthenticationContext = AuthenticationContext.Current;

            context.AuthCookie = GetAuthorizationCookie(context);

            context.OutgoingResponse?.SetCacheControl(noStore: true);

            //if (null == context.AuthCookie)
            //{
            //    context.OutgoingResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
            //    return null;
            //}

            var cont = OnBeforeSignOut(context);

            if (!cont)
            {
                return null;
            }

            if (context.IdTokenHint != null)
            {


                context.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = m_configuration.IssuerName,
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    TryAllIssuerSigningKeys = true,
                    IssuerSigningKeys = GetJsonWebKeySet()?.GetSigningKeys(),
                    NameClaimType = OAuthConstants.ClaimType_Name,
                    ValidAlgorithms = new[] { SignatureAlgorithm.HS256.ToString(), SignatureAlgorithm.RS256.ToString(), SignatureAlgorithm.RS512.ToString() },
                    ValidateIssuerSigningKey = true
                };

                if (!string.IsNullOrEmpty(context.ClientId))
                {
                    context.TokenValidationParameters.ValidAudience = context.ClientId;
                    context.TokenValidationParameters.ValidateAudience = true;
                }

                var validationresult = m_JwtHandler.ValidateToken(context.IdTokenHint, context.TokenValidationParameters);

                if (validationresult?.IsValid == true)
                {
                    var sessionobj = validationresult.Claims.FirstOrDefault(c => c.Key == OAuthConstants.ClaimType_Sid).Value?.ToString();

                    if (null != sessionobj)
                    {
                        ISession session = null;

                        if (Guid.TryParse(sessionobj, out var sessionidguid))
                        {
                            session = m_SessionProvider.Get(sessionidguid.ToByteArray(), allowExpired: false);
                        }
                        else
                        {
                            session = m_SessionProvider.Get(sessionobj.ParseBase64UrlEncode(), allowExpired: false);
                        }

                        if (null != session)
                        {
                            var principal = m_SessionIdentityProvider.Authenticate(session);

                            _AuditService.Audit().ForSessionStop(session, principal, true).Send();
                            m_SessionProvider.Abandon(session);
                            OnAfterSignOut(context);
                            return null;
                        }

                        return CreateErrorResponse(OAuthErrorType.invalid_request, "invalid session");
                    }
                }


                return CreateErrorResponse(OAuthErrorType.invalid_request, "invalid token");
            }
            else
            {
                if (context.AuthCookie?.Users != null)
                {
                    var identityservice = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

                    if (null != identityservice)
                    {
                        foreach (var user in context.AuthCookie.Users)
                        {
                            m_traceSource.TraceVerbose("Abandoning sessions for {0}", user);
                            //Sign the users out of any sessions
                            try
                            {
                                var userid = identityservice.GetSid(user);

                                var identity = identityservice.GetIdentity(user) as IClaimsIdentity;
                                var principal = new SanteDBClaimsPrincipal(identity);


                                var sessions = m_SessionProvider.GetUserSessions(userid);

                                foreach (var session in sessions)
                                {
                                    m_SessionProvider.Abandon(session);
                                    _AuditService.Audit().ForSessionStop(session, principal, true).Send();
                                    context.AbandonedSessions.Add(session);
                                }
                            }
                            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                            {
                                m_traceSource.TraceError("Exception abandoning session for user {0}", ex.ToString());
                            }
                        }
                    }

                }
            }

            OnAfterSignOut(context);

            return null;

        }

        /// <summary>
        /// Invoked before the signout operation is executed.
        /// </summary>
        /// <param name="context">The context for the operation.</param>
        /// <returns><c>false</c> to abort the signout operation. <c>true</c> to continue.</returns>
        protected virtual bool OnBeforeSignOut(OAuthSignoutRequestContext context) => true;

        /// <summary>
        /// Invoked after the signout operation is executed. <see cref="OAuthSignoutRequestContext.AbandonedSessions"/> contains the sessions that were abandoned.
        /// </summary>
        /// <param name="context">The context for the operation.</param>
        protected virtual void OnAfterSignOut(OAuthSignoutRequestContext context)
        {

        }

        /// <inheritdoc/>
        /// TODO: Temporary so the UI can post something to abandon its sessions
        public virtual object Signout()
        {
            return this.Signout(RestOperationContext.Current.IncomingRequest.QueryString);
        }

        #endregion
    }
}

#pragma warning restore

﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Security;

namespace SanteDB.Rest.OAuth
{
    /// <summary>
    /// OAuth constants
    /// </summary>
    public static class OAuthConstants
    {
        public const string ResponseType_Code = "code";
        public const string ResponseType_Token = "token";
        public const string ResponseType_IdToken = "id_token";

        /// <summary>
        /// ACS trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Rest.OAuth";


        /// <summary>
        /// Grant name for the authorization code
        /// </summary>
        public const string GrantNameReset = "x_challenge";

        /// <summary>
        /// Grant name for the authorization code
        /// </summary>
        public const string GrantNameAuthorizationCode = "authorization_code";

        /// <summary>
        /// Grant name for password grant
        /// </summary>
        public const string GrantNamePassword = "password";

        /// <summary>
        /// Grant name for password grant
        /// </summary>
        public const string GrantNameRefresh = "refresh_token";

        /// <summary>
        /// Grant name for client credentials
        /// </summary>
        public const string GrantNameClientCredentials = "client_credentials";

        /// <summary>
        /// JWT token type
        /// </summary>
        public const string JwtTokenType = "urn:ietf:params:oauth:token-type:jwt";

        /// <summary>
        /// Bearer token type
        /// </summary>
        public const string BearerTokenType = "bearer";

        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string ConfigurationName = "santedb.rest.oauth";

        /// <summary>
        /// Gets the client credential policy
        /// </summary>
        public const string OAuthLoginPolicy = PermissionPolicyIdentifiers.LoginAsService + ".0";

        /// <summary>
        /// Client credentials policy
        /// </summary>
        public const string OAuthClientCredentialFlowPolicy = OAuthLoginPolicy + ".1";

        /// <summary>
        /// Client credentials policy without a device authorization present.
        /// </summary>
        public const string OAuthClientCredentialFlowPolicyWithoutDevice = OAuthClientCredentialFlowPolicy + ".0";

        /// <summary>
        /// Password credentials policy
        /// </summary>
        public const string OAuthPasswordFlowPolicy = OAuthLoginPolicy + ".2";

        /// <summary>
        /// Password credentials policy without a device authorization present.
        /// </summary>
        public const string OAuthPasswordFlowPolicyWithoutDevice = OAuthPasswordFlowPolicy + ".0";

        /// <summary>
        /// Code token policy
        /// </summary>
        public const string OAuthCodeFlowPolicy = OAuthLoginPolicy + ".3";

        /// <summary>
        /// Code token policy without a device authorization present.
        /// </summary>
        public const string OAuthCodeFlowPolicyWithoutDevice = OAuthCodeFlowPolicy + ".0";

        /// <summary>
        /// Reset password flow policy.
        /// </summary>
        public const string OAuthResetFlowPolicy = OAuthLoginPolicy + ".4";

        /// <summary>
        /// Reset password flow policy without a device authorization present.
        /// </summary>
        public const string OAuthResetFlowPolicyWithoutDevice = OAuthResetFlowPolicy + ".0";

        /// <summary>
        /// In a token request, this is the grant type field key
        /// </summary>
        public const string FormField_GrantType = "grant_type";
        /// <summary>
        /// In a token request, this is the scope field key
        /// </summary>
        public const string FormField_Scope = "scope";
        /// <summary>
        /// In a token request, this is the client id field key
        /// </summary>
        public const string FormField_ClientId = "client_id";
        /// <summary>
        /// In a token request, this is the client secret field key
        /// </summary>
        public const string FormField_ClientSecret = "client_secret";
        /// <summary>
        /// In a token request, this is the refresh token field key
        /// </summary>
        public const string FormField_RefreshToken = "refresh_token";
        /// <summary>
        /// In a token request, this is the authorization code field key
        /// </summary>
        public const string FormField_AuthorizationCode = "code";
        /// <summary>
        /// In a token request, this is the username field key
        /// </summary>
        public const string FormField_Username = "username";
        /// <summary>
        /// In a token request, this is the password field key
        /// </summary>
        public const string FormField_Password = "password";
        /// <summary>
        /// In a token request with a grant type of x_challenge, this is the challenge key.
        /// </summary>
        public const string FormField_Challenge = "challenge";
        /// <summary>
        /// In a token request with a grant type of x_challenge, this is the response to the challenge.
        /// </summary>
        public const string FormField_ChallengeResponse = "response";

        public const string DataKey_SymmetricSecret = "symm_secret";

        public const string ResponseMode_Query = "query";
        public const string ResponseMode_Fragment = "fragment";
        public const string ResponseMode_FormPost = "form_post";

        public const string AuthorizeParameter_ClientId = "client_id";
        public const string AuthorizeParameter_LoginHint = "login_hint";
        public const string AuthorizeParameter_Nonce = "nonce";
        public const string AuthorizeParameter_Scope = "scope";
        public const string AuthorizeParameter_Prompt = "prompt";
        public const string AuthorizeParameter_State = "state";
        public const string AuthorizeParameter_ResponseType = "response_type";
        public const string AuthorizeParameter_ResponseMode = "response_mode";
        public const string AuthorizeParameter_RedirectUri = "redirect_uri";

    }
}
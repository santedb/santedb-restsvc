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
using SanteDB.Core.Security;

namespace SanteDB.Rest.OAuth
{
    /// <summary>
    /// OAuth constants
    /// </summary>
    public static class OAuthConstants
    {
        /// <summary>
        /// The response type in the authorize request. This is part of the OAuth specification.
        /// </summary>
        public const string ResponseType_Code = "code";
        /// <summary>
        /// The response type in the authorize request. This is part of the OAuth specification.
        /// </summary>
        public const string ResponseType_Token = "token";
        /// <summary>
        /// The response type in the authorize request. This is part of the OAuth specification. 
        /// </summary>
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
        /// The locale form field
        /// </summary>
        public const string FormField_UILocales = "ui_locales";
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
        /// In a token request, this is the tfa field key
        /// </summary>
        public const string FormField_MfaCode = "x_mfa";
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
        /// <summary>
        /// In many requests, the nonce (number once) is used to verify that a request has not been sent more than once or tampered with.
        /// </summary>
        public const string FormField_Nonce = "nonce";

        /// <summary>
        /// Santedb request data key for the symmetric secret that is configured for a particular application.
        /// </summary>
        public const string DataKey_SymmetricSecret = "symm_secret";

        /// <summary>
        /// Returns the response parameters in a querystring to the return url. 
        /// <code>
        /// ...?code=12345&amp;state=0F1A&amp;nonce=12345
        /// </code>
        /// </summary>
        public const string ResponseMode_Query = "query";
        /// <summary>
        /// Returns the response parameters in a url fragment. 
        /// <code>
        /// ...#code=12345&amp;state=0F1A&amp;nonce=12345
        /// </code>
        /// </summary>
        public const string ResponseMode_Fragment = "fragment";
        /// <summary>
        /// Returns the response parameters in a standard form post.
        /// </summary>
        public const string ResponseMode_FormPost = "form_post";

        /// <summary>
        /// Query string property name for the client id.
        /// </summary>
        public const string AuthorizeParameter_ClientId = "client_id";
        /// <summary>
        /// Query string property name for the login hint.
        /// </summary>
        public const string AuthorizeParameter_LoginHint = "login_hint";
        /// <summary>
        /// Query string property name for the nonce.
        /// </summary>
        public const string AuthorizeParameter_Nonce = "nonce";
        /// <summary>
        /// Query string property name for the scope.
        /// </summary>
        public const string AuthorizeParameter_Scope = "scope";
        /// <summary>
        /// Query string property name for the prompt.
        /// </summary>
        public const string AuthorizeParameter_Prompt = "prompt";
        /// <summary>
        /// Query string property name for the state.
        /// </summary>
        public const string AuthorizeParameter_State = "state";
        /// <summary>
        /// Query string property name for the response type.
        /// </summary>
        public const string AuthorizeParameter_ResponseType = "response_type";
        /// <summary>
        /// Query string property name for the response mode.
        /// </summary>
        public const string AuthorizeParameter_ResponseMode = "response_mode";
        /// <summary>
        /// Query string property name for the redirect uri.
        /// </summary>
        public const string AuthorizeParameter_RedirectUri = "redirect_uri";


        //Signout endpoint.
        /// <summary>
        /// An id token used to select which user to sign out.
        /// </summary>
        public const string FormField_IdTokenHint = "id_token_hint";
        /// <summary>
        /// The user identifier used to determine who to log out.
        /// </summary>
        public const string FormField_LogoutHint = "logout_hint";
        /// <summary>
        /// Where to redirect the user agent after the sign out process is completed.
        /// </summary>
        public const string FormField_PostLogoutRedirectUri = "post_logout_redirect_uri";

        /// <summary>
        /// Name
        /// </summary>
        public const string ClaimType_Name = "name";
        /// <summary>
        /// Actor
        /// </summary>
        public const string ClaimType_Actor = "actor";
        /// <summary>
        /// Subject
        /// </summary>
        public const string ClaimType_Subject = "sub";
        /// <summary>
        /// Session Id
        /// </summary>
        public const string ClaimType_Sid = "sid";
        /// <summary>
        /// Nonce
        /// </summary>
        public const string ClaimType_Nonce = "nonce";
        /// <summary>
        /// Access token hash
        /// </summary>
        public const string ClaimType_AtHash = "at_hash";
        /// <summary>
        /// JWT id
        /// </summary>
        public const string ClaimType_Jti = "jti";
        /// <summary>
        /// Role
        /// </summary>
        public const string ClaimType_Role = "role";
        /// <summary>
        /// Email
        /// </summary>
        public const string ClaimType_Email = "email";
        /// <summary>
        /// Realm
        /// </summary>
        public const string ClaimType_Realm = "realm";
        /// <summary>
        /// Phone Number
        /// </summary>
        public const string ClaimType_Telephone = "phone_number";

        // Claims from https://profiles.ihe.net/ITI/IUA/index.html#3714221-json-web-token-option
        /// <summary>
        /// IUA Subject Name
        /// </summary>
        public const string IUA_Claim_SubjectName = "subject_name";
        /// <summary>
        /// Facility id
        /// </summary>
        public const string IUA_Claim_FacilityId = "subject_facility_id";
        /// <summary>
        /// IUA Subject Organization ID
        /// </summary>
        public const string IUA_Claim_SubjectOrganizationId = "subject_organization_id";
        /// <summary>
        /// IUA Subject Organization
        /// </summary>
        public const string IUA_Claim_SubjectOrganization = "subject_organization";
        /// <summary>
        /// IUA Subject Role
        /// </summary>
        public const string IUA_Claim_SubjectRole = "subject_role";
        /// <summary>
        /// IUA Purpose of Use
        /// </summary>
        public const string IUA_Claim_PurposeOfUse = "purpose_of_use";
        /// <summary>
        /// IUA Nation Provider Id
        /// </summary>
        public const string IUA_Claim_NationalProviderId = "national_provider_identifier";
        /// <summary>
        /// IUA Person Id
        /// </summary>
        public const string IUA_Claim_PersonId = "person_id";
    }
}

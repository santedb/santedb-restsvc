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
 * Date: 2023-3-10
 */
using Microsoft.IdentityModel.Tokens;
using RestSrvr;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Rest.OAuth.Configuration;
using System;
using System.Collections.Specialized;
using System.Net;

namespace SanteDB.Rest.OAuth.Model
{
    /// <summary>
    /// Base class for OAuth requests.
    /// </summary>
    public abstract class OAuthRequestContextBase
    {
        public OAuthRequestContextBase(RestOperationContext operationContext, NameValueCollection formFields)
            : this(operationContext)
        {
            FormFields = formFields;
        }

        public OAuthRequestContextBase(RestOperationContext operationContext)
        {
            OperationContext = operationContext;
        }

        #region Core Properties
        public NameValueCollection FormFields { get; }

        public RestOperationContext OperationContext { get; }

        public HttpListenerRequest IncomingRequest => OperationContext?.IncomingRequest;
        public HttpListenerResponse OutgoingResponse => OperationContext?.OutgoingResponse;
        #endregion

        #region Request Header Values
        /// <summary>
        /// A secret code used as a second factor in an authentication flow.
        /// </summary>
        [Obsolete("Use of this is discouraged.")]
        public string TfaSecret => FormFields?[OAuthConstants.FormField_MfaCode];

        #endregion

        #region Common Request Elements
        /// <summary>
        /// Username form field. Applicable during an authorize login or from a token post with a grant type of password.
        /// </summary>
        public string Username => FormFields?[OAuthConstants.FormField_Username];
        /// <summary>
        /// Password form field. Applicable during an authorize login or from a token post with a grant type of password.
        /// </summary>
        public string Password => FormFields?[OAuthConstants.FormField_Password];
        #endregion

        /// <summary>
        /// When a request fails, this should contain the type of error that was encountered.
        /// </summary>
        public OAuthErrorType? ErrorType { get; set; }

        /// <summary>
        /// When a request fails, this should contain a textual description that will be returned in the response.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The config section that is applicable during processing of the request.
        /// </summary>
        public OAuthConfigurationSection Configuration { get; set; }

        /// <summary>
        /// Gets or sets the authentication context at the time the handler is processing a request.
        /// </summary>
        public AuthenticationContext AuthenticationContext { get; set; }
        /// <summary>
        /// The authenticated device identity.
        /// </summary>
        public IClaimsIdentity DeviceIdentity { get; set; }
        /// <summary>
        /// The authenticated device principal.
        /// </summary>
        public IClaimsPrincipal DevicePrincipal { get; set; }
        /// <summary>
        /// The authenticated application identity.
        /// </summary>
        public IClaimsIdentity ApplicationIdentity { get; set; }
        /// <summary>
        /// The authenticated application principal.
        /// </summary>
        public IClaimsPrincipal ApplicationPrincipal { get; set; }
        /// <summary>
        /// The authenticated user identity.
        /// </summary>
        public IClaimsIdentity UserIdentity { get; set; }
        /// <summary>
        /// The authenticated user principal.
        /// </summary>
        public IClaimsPrincipal UserPrincipal { get; set; }

        public virtual string ClientId => null;
        public virtual string ClientSecret => null;
        /// <summary>
        /// The session that is established as part of this request. Typically, an <see cref="Abstractions.ITokenRequestHandler"/> will set this during processing.
        /// </summary>
        public ISession Session { get; set; }
        /// <summary>
        /// A token descriptor that is generated as part of this request.
        /// </summary>
        public SecurityTokenDescriptor SecurityTokenDescriptor { get; set; }
        /// <summary>
        /// An access token that is created as part of the reuqest.
        /// </summary>
        public string AccessToken { get; set; }
        /// <summary>
        /// An id token that is created as part of the request.
        /// </summary>
        public string IdToken { get; set; }
        /// <summary>
        /// If there is an access token, when the access token expires.
        /// </summary>
        public TimeSpan ExpiresIn { get; set; }
        /// <summary>
        /// The type of access token in the request.
        /// </summary>
        public string TokenType { get; set; }
        /// <summary>
        /// A nonce value that was part of a flow.
        /// </summary>
        public virtual string Nonce { get; set; }
    }
}

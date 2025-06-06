﻿/*
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
using Microsoft.IdentityModel.Tokens;
using RestSrvr;
using SanteDB.Core.Security;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SanteDB.Rest.OAuth.Model
{
    /// <summary>
    /// Request context for a signout request.
    /// </summary>
    public class OAuthSignoutRequestContext : OAuthRequestContextBase
    {
        /// <inheritdoc />
        public OAuthSignoutRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }
        /// <inheritdoc />
        public OAuthSignoutRequestContext(RestOperationContext operationContext, NameValueCollection formFields) : base(operationContext, formFields)
        {
        }

        /// <summary>
        /// The ID token of the session that the user would like to sign out of.
        /// </summary>
        public string IdTokenHint => FormFields[OAuthConstants.FormField_IdTokenHint];
        /// <summary>
        /// The user that the request is attempting to sign out. Valid if multiple users are established with the provider.
        /// </summary>
        public string LogoutHint => FormFields[OAuthConstants.FormField_LogoutHint];
        /// <summary>
        /// Where to redirect the user agent after a signout is complete.
        /// </summary>
        public string PostLogoutRedirectUri => FormFields[OAuthConstants.FormField_PostLogoutRedirectUri];

        /// <summary>
        /// A list of sessions that were abandoned. This allows derived implementations to do their own cleanup.
        /// </summary>
        public List<ISession> AbandonedSessions => new List<ISession>();
        /// <summary>
        /// The authorization cookie that exists as part of the user/app's authenticated context.
        /// </summary>
        public AuthorizationCookie AuthCookie { get; set; }

        /// <summary>
        /// Token validation parameters when an ID token is specified.
        /// </summary>
        public TokenValidationParameters TokenValidationParameters { get; set; }
    }
}

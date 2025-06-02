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
using RestSrvr;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.OAuth.Model
{
    internal class OAuthAuthorizeRequestContext : OAuthRequestContextBase
    {
        public OAuthAuthorizeRequestContext(RestOperationContext operationContext, NameValueCollection formFields)
             : base(operationContext, formFields)
        {
        }

        public OAuthAuthorizeRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }

        private string GetValue(string key) => FormFields?[key] ?? IncomingRequest?.QueryString?[key];


        public override string ClientId => GetValue(OAuthConstants.AuthorizeParameter_ClientId);
        public string LoginHint => GetValue(OAuthConstants.AuthorizeParameter_LoginHint);

        public override string Nonce
        {
            get => base.Nonce ?? GetValue(OAuthConstants.AuthorizeParameter_Nonce);
            set => base.Nonce = value;
        }

        public string Scope => GetValue(OAuthConstants.AuthorizeParameter_Scope);
        public string Prompt => GetValue(OAuthConstants.AuthorizeParameter_Prompt);
        public string State => GetValue(OAuthConstants.AuthorizeParameter_State);


        private string _ResponseType;
        public string ResponseType
        {
            get => _ResponseType ?? GetValue(OAuthConstants.AuthorizeParameter_ResponseType);
            set => _ResponseType = value;
        }

        private string _ResponseMode;
        public string ResponseMode
        {
            get => _ResponseMode ?? GetValue(OAuthConstants.AuthorizeParameter_ResponseMode);
            set => _ResponseMode = value;
        }
        public string RedirectUri => GetValue(OAuthConstants.AuthorizeParameter_RedirectUri);



        public Guid ActivityId
        {
            get
            {
                var val = OperationContext?.Data?["uuid"];

                if (val is Guid g)
                {
                    return g;
                }

                return Guid.Empty;
            }
        }

        /// <summary>
        /// The code that was generated for the response.
        /// </summary>
        public string Code { get; set; }
    }
}

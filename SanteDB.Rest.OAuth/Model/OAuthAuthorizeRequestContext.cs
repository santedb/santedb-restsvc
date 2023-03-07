﻿using RestSrvr;
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

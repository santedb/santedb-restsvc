﻿using RestSrvr;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.OAuth.Model
{
    public class OAuthSignoutRequestContext : OAuthRequestContextBase
    {
        public OAuthSignoutRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }

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
    }
}
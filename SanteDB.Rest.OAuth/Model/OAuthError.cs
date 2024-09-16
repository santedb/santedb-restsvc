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
 */
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SanteDB.Rest.OAuth.Model
{
    /// <summary>
    /// OAuth error type
    /// </summary>
    public enum OAuthErrorType
    {
        /// <summary>
        /// The client sent an invalid request
        /// </summary>
        invalid_request,
        /// <summary>
        /// The client secret is invalid
        /// </summary>
        invalid_client,
        /// <summary>
        /// The grant information was invalid (secret or password)
        /// </summary>
        invalid_grant,
        /// <summary>
        /// The client is not authorized to perform the action
        /// </summary>
        unauthorized_client,
        /// <summary>
        /// The grant type is not supported by this server
        /// </summary>
        unsupported_grant_type,
        /// <summary>
        /// The scope value is invalid
        /// </summary>
        invalid_scope,
        /// <summary>
        /// The type of error was not specified by a handler.
        /// </summary>
        unspecified_error,
        /// <summary>
        /// The response type requested is not supported by this service.
        /// </summary>
        unsupported_response_type,
        /// <summary>
        /// The response mode request is not supported by this service.
        /// </summary>
        unsupported_response_mode,
        /// <summary>
        /// The requested resource requires mfa.
        /// </summary>
        mfa_required,
        /// <summary>
        /// Password is expired
        /// </summary>
        password_expired,
        /// <summary>
        /// User has attempted to establish a session but has not provided a required claim - error_description contains the missing claim
        /// </summary>
        missing_claim
    }

    /// <summary>
    /// OAuth error response message
    /// </summary>
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class OAuthError
    {

        /// <summary>
        /// Gets or sets the error
        /// </summary>
        [JsonProperty("error"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public OAuthErrorType Error { get; set; }

        /// <summary>
        /// Description of the error
        /// </summary>
        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Gets or sets the state that the client provided in the request.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Error detail - extended field for sending special data back
        /// </summary>
        [JsonProperty("error_detail")]
        public String ErrorDetail { get; set; }

        /// <summary>
        /// Error data
        /// </summary>
        [JsonProperty("data")]
        public IDictionary<String, Object> ErrorData { get; set; }
    }
}

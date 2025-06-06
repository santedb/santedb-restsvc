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
using System;

namespace SanteDB.Rest.OAuth.Model
{
    internal class AuthorizationCode
    {
        /// <summary>
        /// Time <see cref="AuthorizationCode"/> was generated by the server.
        /// </summary>
        public DateTimeOffset iat { get; set; }
        /// <summary>
        /// Device  sid.
        /// </summary>
        public string dev { get; set; }
        /// <summary>
        /// Application or client sid.
        /// </summary>
        public string app { get; set; }
        /// <summary>
        /// User sid.
        /// </summary>
        public string usr { get; set; }
        /// <summary>
        /// Nonce provided in request and returned in token response.
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// Scopes authorized.
        /// </summary>
        public string scp { get; set; }
        /// <summary>
        /// code_verifier parameter for PKCE based flows.
        /// </summary>
        public string cv { get; set; }
        /// <summary>
        /// code_verifier method for PKCE based flows.
        /// </summary>
        public string cvm { get; set; }
    }
}

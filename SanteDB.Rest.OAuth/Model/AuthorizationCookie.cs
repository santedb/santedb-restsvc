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
using System.Collections.Generic;

namespace SanteDB.Rest.OAuth.Model
{
    /// <summary>
    /// Data structure for serializing user information into a cookie.
    /// </summary>
    public class AuthorizationCookie
    {
        /// <summary>
        /// Users covered in the cookie.
        /// </summary>
        [JsonProperty("u")]
        public List<string> Users { get; set; }
        /// <summary>
        /// When the cookie was created according to the server.
        /// </summary>
        [JsonProperty("c")]
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>
        /// Random nonce value for the cookie.
        /// </summary>
        [JsonProperty("n")]
        public int Nonce { get; set; }
    }
}

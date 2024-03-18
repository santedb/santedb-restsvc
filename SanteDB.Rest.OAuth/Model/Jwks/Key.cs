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
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SanteDB.Rest.OAuth.Model.Jwks
{
    /// <summary>
    /// Serialization class for the jwks endpoint. This hides all the internal details from the Key.
    /// </summary>
    internal class Key
    {
        [JsonProperty("alg")]
        public string Algorithm { get; set; }
        [JsonProperty("kty")]
        public string KeyType { get; set; }
        [JsonProperty("use")]
        public string Use { get; set; }
        [JsonProperty("x5c")]
        public IList<string> CertificateChain { get; set; }
        [JsonProperty("n")]
        public string Modulus { get; set; }
        [JsonProperty("e")]
        public string Exponent { get; set; }
        [JsonProperty("kid")]
        public string KeyId { get; set; }
        [JsonProperty("x5t")]
        public string Thumbprint { get; set; }
        [JsonProperty("x5t#S256")]
        public string ThumbprintSHA256 { get; set; }
        [JsonProperty("key_ops")]
        public IList<string> KeyOperations { get; set; }
        [JsonProperty("x5u")]
        public string CertificateUrl { get; set; }
        [JsonProperty("k")]
        public string K { get; set; }

        public bool ShouldSerializeCertificateChain() => CertificateChain.Count > 0;
        public bool ShouldSerializeKeyOperations() => KeyOperations.Count > 0;
    }
}

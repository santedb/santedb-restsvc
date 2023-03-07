using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.OAuth.Model
{
    public class AuthorizationCookie
    {
        [JsonProperty("u")]
        public List<string> Users { get; set; }
        [JsonProperty("c")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonProperty("n")]
        public int Nonce { get; set; }
    }
}

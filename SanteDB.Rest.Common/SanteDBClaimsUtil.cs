/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-12-24
 */
using SanteDB.Core.Security.Claims;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// SanteDB Claim Types
    /// </summary>
    public static class SanteDBClaimsUtil
    {

        /// <summary>
        /// Static ctor
        /// </summary>
        static SanteDBClaimsUtil()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    foreach (var t in asm.GetTypes().Where(o => typeof(IClaimTypeHandler).IsAssignableFrom(o) && o.IsClass))
                    {
                        IClaimTypeHandler handler = t.GetConstructor(Type.EmptyTypes).Invoke(null) as IClaimTypeHandler;
                        s_claimHandlers.Add(handler.ClaimType, handler);
                    }
                }
                catch { }
        }

        /// <summary>
        /// A list of claim handlers
        /// </summary>
        private static Dictionary<String, IClaimTypeHandler> s_claimHandlers = new Dictionary<string, IClaimTypeHandler>();

        
        /// <summary>
        /// Gets the specified claim type handler
        /// </summary>
        public static IClaimTypeHandler GetHandler(String claimType)
        {
            IClaimTypeHandler handler = null;
            s_claimHandlers.TryGetValue(claimType, out handler);
            return handler;
        }

        /// <summary>
        /// Extract claims
        /// </summary>
        public static List<IClaim> ExtractClaims(NameValueCollection headers)
        {
            var claimsHeaders = headers[SanteDBRestConstants.BasicHttpClientClaimHeaderName];
            if (claimsHeaders == null)
                return new List<IClaim>();
            else
            {
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(claimsHeaders));
                return data.Split(';').Select(o => o.Split('=')).Select(c => new SanteDBClaim(c[0], c[1])).OfType<IClaim>().ToList();
            }
        } 
    }
}

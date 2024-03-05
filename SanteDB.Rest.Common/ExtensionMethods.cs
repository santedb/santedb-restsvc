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
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Extension methods
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ExtensionMethods
    {

        /// <summary>
        /// Encode the specified string to ASCII escape characters
        /// </summary>
        public static String EncodeAscii(this string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in value)
            {
                if (c > 127)
                {
                    sb.AppendFormat("\\u{0:x4}", (int)c);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get if modified since
        /// </summary>
        public static DateTime? GetIfModifiedSince(this HttpListenerRequest me)
        {
            if (me.Headers["If-Modified-Since"] != null)
            {
                return DateTime.Parse(me.Headers["If-Modified-Since"]);
            }

            return null;
        }

        /// <summary>
        /// Get if modified since
        /// </summary>
        public static DateTime? GetIfUnmodifiedSince(this HttpListenerRequest me)
        {
            if (me.Headers["If-Unmodified-Since"] != null)
            {
                return DateTime.Parse(me.Headers["If-Unmodified-Since"]);
            }

            return null;
        }

        /// <summary>
        /// Get if non match header
        /// </summary>
        public static String[] GetIfNoneMatch(this HttpListenerRequest me)
        {
            return me.Headers["If-None-Match"]?.ParseETagMatch();
        }

        /// <summary>
        /// Get the if match header
        /// </summary>
        public static string[] GetIfMatch(this HttpListenerRequest me)
        {
            return me.Headers["If-Match"]?.ParseETagMatch();
        }

        /// <summary>
        /// Parse ETag match header
        /// </summary>
        private static string[] ParseETagMatch(this String me) => me.Split('"').Where(o =>
                        !o.StartsWith(",") &&
                        !"W/".Equals(o) &&
                        !String.IsNullOrEmpty(o)).ToArray();

        /// <summary>
        /// Set the e-tag
        /// </summary>
        /// <param name="me">The response to set the e-tag header on.</param>
        /// <param name="isWeak">True if the e-tag is a weak reference</param>
        /// <param name="etag">The value of the e-tag</param>
        public static void SetETag(this HttpListenerResponse me, String etag, bool isWeak = false)
        {
            if (!String.IsNullOrEmpty(etag))
            {
                me.AppendHeader("ETag", isWeak ? $"W/{etag}" : etag);
            }
        }

        /// <summary>
        /// Set last modified time
        /// </summary>
        /// <param name="me"></param>
        /// <param name="lastModified"></param>
        public static void SetLastModified(this HttpListenerResponse me, DateTime lastModified)
        {
            me.AppendHeader("Last-Modified", lastModified.ToString("r"));
        }


    }
}

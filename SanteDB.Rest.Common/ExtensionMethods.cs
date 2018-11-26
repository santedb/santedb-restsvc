/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-20
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ExtensionMethods
    {

        /// <summary>
        /// Get if modified since
        /// </summary>
        public static DateTime? GetIfModifiedSince(this HttpListenerRequest me)
        {
            if (me.Headers["If-Modified-Since"] != null)
                return DateTime.Parse(me.Headers["If-Modified-Since"]);
            return null;
        }

        /// <summary>
        /// Get if non match header
        /// </summary>
        public static String[] GetIfNoneMatch(this HttpListenerRequest me)
        {
            if (me.Headers["If-None-Match"] != null)
                return me.Headers["If-None-Match"].Split(',');
            return null;
        }

        /// <summary>
        /// Set the e-tag
        /// </summary>
        /// <param name="me"></param>
        /// <param name="etag"></param>
        public static void SetETag(this HttpListenerResponse me, String etag)
        {
            if (!String.IsNullOrEmpty(etag))
                me.AppendHeader("ETag", etag);
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

        /// <summary>
        /// Convert query types
        /// </summary>
        public static SanteDB.Core.Model.Query.NameValueCollection ToQuery(this System.Collections.Specialized.NameValueCollection nvc)
        {
            var retVal = new SanteDB.Core.Model.Query.NameValueCollection();
            foreach (var k in nvc.AllKeys)
                retVal.Add(k, new List<String>(nvc.GetValues(k)));
            return retVal;
        }

        /// <summary>
        /// Convert query types
        /// </summary>
        public static List<KeyValuePair<String, Object>> ToList(this System.Collections.Specialized.NameValueCollection nvc)
        {
            var retVal = new List<KeyValuePair<String, Object>>();
            foreach (var k in nvc.AllKeys)
                foreach(var v in nvc.GetValues(k))
                    retVal.Add(new KeyValuePair<String, Object>(k, v));
            return retVal;
        }

        /// <summary>
        /// Adds a handler to the Started event
        /// </summary>
        public static void AddStarted(this IServiceProvider me, EventHandler handler)
        {
            var startEvent = me.GetType().GetRuntimeEvent("Started");
            startEvent.AddEventHandler(me, handler);
        }
    }
}

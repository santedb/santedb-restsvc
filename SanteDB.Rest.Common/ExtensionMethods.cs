using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
                me.SetETag(etag);
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
        /// Adds a handler to the Started event
        /// </summary>
        public static void AddStarted(this IServiceProvider me, EventHandler handler)
        {
            var startEvent = me.GetType().GetRuntimeEvent("Started");
            startEvent.AddEventHandler(me, handler);
        }
    }
}

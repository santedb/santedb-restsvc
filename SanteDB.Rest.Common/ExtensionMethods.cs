using System;
using System.Collections.Generic;
using System.Linq;
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

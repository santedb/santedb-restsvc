using RestSrvr.Attributes;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common.Attributes
{
    /// <summary>
    /// Represents a common rest service fault
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class RestServiceFaultAttribute : ServiceFaultAttribute
    {

        /// <summary>
        /// Creates a new rest service fault attribute
        /// </summary>
        public RestServiceFaultAttribute(int statusCode, string condition) : base(statusCode, typeof(RestServiceFault), condition)
        {

        }
    }
}

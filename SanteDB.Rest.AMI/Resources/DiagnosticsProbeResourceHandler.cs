using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Performance counter handler
    /// </summary>
    public class DiagnosticsProbeResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Probe";

        /// <summary>
        /// Gets the type that this handler returns
        /// </summary>
        public Type Type => typeof(DiagnosticsProbe);

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;
        

        /// <summary>
        /// Create an entity
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the specified performance counter
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Get(object id, object versionId)
        {
            return new DiagnosticsProbeReading(DiagnosticsProbeManager.Current.Get((Guid)id));
        }

        /// <summary>
        /// Delete the probe
        /// </summary>
        public object Obsolete(object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for probes
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return this.Query(queryParameters, 0, 100, out int tr);
        }

        /// <summary>
        /// Query for probes
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<IDiagnosticsProbe>(queryParameters);
                return DiagnosticsProbeManager.Current.Find(filter.Compile(), offset, count, out totalCount).Select(o=>new DiagnosticsProbe(o));
            }
            catch(Exception e)
            {
                throw new Exception($"Error querying probes : {e.Message}", e);
            }
        }
        
        /// <summary>
        /// Update the object
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}

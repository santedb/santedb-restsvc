/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
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
                return DiagnosticsProbeManager.Current.Find(filter.Compile(), offset, count, out totalCount).Select(o => new DiagnosticsProbe(o));
            }
            catch (Exception e)
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

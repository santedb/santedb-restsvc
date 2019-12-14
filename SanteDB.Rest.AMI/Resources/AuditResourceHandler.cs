/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: justi
 * Date: 2019-1-12
 */
using SanteDB.Core;
using SanteDB.Core.Auditing;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which can persist and forward audits
    /// </summary>
    public class AuditResourceHandler : IApiResourceHandler
    {

        // The audit repository
        private IAuditRepositoryService m_repository = null;

        /// <summary>
        /// Initializes the audit resource handler
        /// </summary>
        public AuditResourceHandler()
        {
            ApplicationServiceContext.Current.AddStarted((o, e) => this.m_repository = ApplicationServiceContext.Current.GetService<IAuditRepositoryService>()); 
        }

        /// <summary>
        /// Get the capabilities of this handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string ResourceName => "Audit";

        /// <summary>
        /// Get the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type this persists
        /// </summary>
        public Type Type => typeof(AuditData);

        /// <summary>
        /// Create the audits in the audit data
        /// </summary>
        /// <param name="data">The audit data to send/insert</param>
        /// <param name="updateIfExists">Ignored for this provider</param>
        /// <returns>void</returns>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Create(object data, bool updateIfExists)
        {
            if (this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var auditData = data as AuditSubmission;
            if (auditData == null) // may be a single audit
            {
                var singleAudit = data as AuditData;
                if (singleAudit != null)
                {
                    var retVal = this.m_repository.Insert(singleAudit);
                    ApplicationServiceContext.Current.GetService<IAuditDispatchService>()?.SendAudit(singleAudit);
                    return new AuditData().CopyObjectData(retVal);
                }
            }
            else
            {
                auditData.Audit.ForEach(o =>
                {
                    this.m_repository.Insert(o);
                    ApplicationServiceContext.Current.GetService<IAuditDispatchService>()?.SendAudit(o);
                });
                // Send the audit to the audit repo
            }
            return null;
        }

        /// <summary>
        /// Get the specified audit identifier from the database
        /// </summary>
        /// <param name="id">The identifier of the audit to retrieve</param>
        /// <param name="versionId">Ignored</param>
        /// <returns>The fetched audit information</returns>
        [Demand(PermissionPolicyIdentifiers.AccessAuditLog)]
        public object Get(object id, object versionId)
        {
            if (this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var retVal = new AuditData();
            retVal.CopyObjectData(this.m_repository.Get(id));

            return retVal;
        }

        /// <summary>
        /// Obsolete the audit
        /// </summary>
        /// <param name="key">Not supported</param>
        /// <returns></returns>
        public object Obsolete(object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for the audit 
        /// </summary>
        /// <param name="queryParameters">The query to perform</param>
        /// <returns>The matching audits</returns>
        [Demand(PermissionPolicyIdentifiers.AccessAuditLog)]
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Perform the query for audits
        /// </summary>
        /// <param name="queryParameters">The filter parameters for the audit</param>
        /// <param name="offset">The first result to retrieve</param>
        /// <param name="count">The count of objects to retrieve</param>
        /// <param name="totalCount">The total couns of objects</param>
        /// <returns>An array of objects matching the query </returns>
        [Demand(PermissionPolicyIdentifiers.AccessAuditLog)]
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            if(this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var filter = QueryExpressionParser.BuildLinqExpression<AuditData>(queryParameters);
            return this.m_repository.Find(filter, offset, count, out totalCount);
        }

        /// <summary>
        /// Perform an update on an audit
        /// </summary>
        /// <param name="data">The data to be updated</param>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}

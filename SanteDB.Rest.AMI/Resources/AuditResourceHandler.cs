/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Audit submission resource
    /// </summary>
    public class AuditSubmissionResourceHandler : AuditResourceHandler
    {
        /// <inheritdoc/>
        public AuditSubmissionResourceHandler(ILocalizationService localizationService, IRepositoryService<AuditEventData> repositoryService, IAuditDispatchService dispatchService = null) : base(localizationService, repositoryService, dispatchService)
        {
        }

        /// <inheritdoc/>
        public override string ResourceName => "AuditSubmission";

        /// <inheritdoc/>
        public override Type Type => typeof(AuditSubmission);

    }

    /// <summary>
    /// Represents a resource handler which can persist and forward audits
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AuditResourceHandler : IServiceImplementation, IApiResourceHandler
    {
        // Configuration
        private AuditAccountabilityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AuditAccountabilityConfigurationSection>();

        // The audit repository
        private IRepositoryService<AuditEventData> m_repository = null;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AuditResourceHandler));

        // Audit dispatch
        private readonly IAuditDispatchService m_auditDispatch;

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Initializes the audit resource handler
        /// </summary>
        public AuditResourceHandler(ILocalizationService localizationService, IRepositoryService<AuditEventData> repositoryService, IAuditDispatchService dispatchService = null)
        {
            this.m_localizationService = localizationService;
            this.m_repository = repositoryService;
            this.m_auditDispatch = dispatchService;
        }

        /// <summary>
        /// Get the capabilities of this handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        // Dispatcher
        private IAuditDispatchService m_dispatcher = ApplicationServiceContext.Current.GetService<IAuditDispatchService>();

        /// <summary>
        /// The name of the resource
        /// </summary>
        public virtual string ResourceName => "Audit";

        /// <summary>
        /// Get the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type this persists
        /// </summary>
        public virtual Type Type => typeof(AuditEventData);

        /// <summary>
        /// Get the service name
        /// </summary>
        public string ServiceName => "Audit Resource Handler";

        /// <summary>
        /// Create the audits in the audit data
        /// </summary>
        /// <param name="data">The audit data to send/insert</param>
        /// <param name="updateIfExists">Ignored for this provider</param>
        /// <returns>void</returns>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Create(object data, bool updateIfExists)
        {
            var auditData = data as AuditSubmission;
            if (auditData == null) // may be a single audit
            {
                var singleAudit = data as AuditEventData;
                if (singleAudit != null)
                {
                    var retVal = this.m_repository.Insert(singleAudit);
                    ApplicationServiceContext.Current.GetService<IAuditDispatchService>()?.SendAudit(singleAudit);
                    return null;
                }
            }
            else
            {
                auditData.Audit?.ForEach(o =>
                {
                    this.m_repository.Insert(o);
                    this.m_auditDispatch?.SendAudit(o);
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
            var retVal = new AuditEventData();
            if (Guid.TryParse(id.ToString(), out Guid gid))
            {
                retVal.CopyObjectData(this.m_repository.Get(gid));
            }

            return retVal;
        }

        /// <summary>
        /// Obsolete the audit
        /// </summary>
        /// <param name="key">Not supported</param>
        /// <returns></returns>
        public object Delete(object key)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Perform the query for audits
        /// </summary>
        /// <param name="queryParameters">The filter parameters for the audit</param>
        /// <returns>An array of objects matching the query </returns>
        [Demand(PermissionPolicyIdentifiers.AccessAuditLog)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var filter = QueryExpressionParser.BuildLinqExpression<AuditEventData>(queryParameters);
            return this.m_repository.Find(filter);
        }

        /// <summary>
        /// Perform an update on an audit
        /// </summary>
        /// <param name="data">The data to be updated</param>
        public object Update(object data)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }
    }
}
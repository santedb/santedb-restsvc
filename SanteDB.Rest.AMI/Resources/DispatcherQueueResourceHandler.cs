/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using RestSrvr;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents the primary queue resource handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DispatcherQueueResourceHandler : ChainedResourceHandlerBase
    {


        // Queue service
        private readonly IDispatcherQueueManagerService m_queueService;

        readonly IAuditService _AuditService;

        /// <summary>
        /// DI constructor for persistent queue
        /// </summary>
        public DispatcherQueueResourceHandler(IDispatcherQueueManagerService queueService, ILocalizationService localization, IAuditService auditService) : base(localization)
        {
            this.m_queueService = queueService;
            _AuditService = auditService;
        }

        /// <summary>
        /// Get the name of the resource
        /// </summary>
        public override string ResourceName => nameof(DispatcherQueueInfo);

        /// <summary>
        /// Gets the type
        /// </summary>
        public override Type Type => typeof(DispatcherQueueInfo);

        /// <summary>
        /// Gets the scope of the resource
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of the object
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Update;

        /// <summary>
        /// Create not supported
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get a specific queue entry
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object Get(object id, object versionId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obsolete the specified queue - not supported
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object Delete(object key)
        {
            this.m_queueService.Purge((String)key);
            _AuditService.Audit()
                .WithLocalDestination()
                   .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
                .WithPrincipal()
                .WithAction(Core.Model.Audit.ActionType.Delete)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                .WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                .WithTimestamp(DateTimeOffset.Now)
                .WithEventType("PurgeQueue")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.Resource, Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, new Uri($"urn:santedb:org:DispatcherQueueInfo/{key}/event"))
                .Send();
            return null;
        }

        /// <summary>
        /// Query for all
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var queues = this.m_queueService.GetQueues();

            if (queryParameters.TryGetValue("name", out var nameFilter))
            {
                queues = queues.Where(o => o.Name.Contains(nameFilter.First().Replace("*", "").Replace("%", "")));
            }
            return new MemoryQueryResultSet(queues);
        }

        /// <summary>
        /// Update the queue (not supported)
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object Update(object data)
        {
            if (data is DispatcherQueueInfo dqe)
            {
                // The updated objects are the source queue,
                var toQueue = RestOperationContext.Current.IncomingRequest.QueryString["_to"];
                if (String.IsNullOrEmpty(toQueue))
                {
                    toQueue = dqe.Id.Replace(".dead", "");
                }

                foreach (var itm in this.m_queueService.GetQueueEntries(dqe.Id))
                {
                    this.m_queueService.Move(itm, toQueue);
                }
            }
            return null;
        }

        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            return base.RemoveChildObject(scopingEntityKey, propertyName, subItemKey);
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            return base.QueryChildObjects(scopingEntityKey, propertyName, filter);
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            return base.AddChildObject(scopingEntityKey, propertyName, scopedItem);
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public override object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            return this.GetChildObject(scopingEntity, propertyName, subItemKey);
        }

    }
}
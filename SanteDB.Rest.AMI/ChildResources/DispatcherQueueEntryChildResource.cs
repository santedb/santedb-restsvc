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
using RestSrvr;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Dispatcher queue entry child resource
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DispatcherQueueEntryChildResource : IApiChildResourceHandler
    {
        // Queue service
        private readonly IDispatcherQueueManagerService m_queueService;

        // Localization service
        private readonly ILocalizationService m_localizationService;

        // PEP service
        private readonly IPolicyEnforcementService m_policyEnforcementService;

        //Audit Service
        readonly IAuditService _AuditService;

        /// <summary>
        /// DI constructor for persistent queue
        /// </summary>
        public DispatcherQueueEntryChildResource(IDispatcherQueueManagerService queueService, ILocalizationService localization, IPolicyEnforcementService pepService, IAuditService auditService)
        {
            this.m_queueService = queueService;
            this.m_localizationService = localization;
            this.m_policyEnforcementService = pepService;
            _AuditService = auditService;
        }

        /// <summary>
        /// Gets the type of the property
        /// </summary>
        public Type PropertyType => typeof(DispatcherQueueEntry);

        /// <summary>
        /// Get the capabilities of this service
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Update | ResourceCapabilityType.Create | ResourceCapabilityType.Search;

        /// <summary>
        /// Gets the scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(DispatcherQueueInfo) };

        /// <summary>
        /// Get the name of this sub-resource
        /// </summary>
        public string Name => "entry";

        /// <summary>
        /// Adds an entry - or rather - moves it
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            if (item is DispatcherQueueEntry dqe)
            {
                var retVal = this.m_queueService.Move(dqe, (string)scopingKey);
                _AuditService.Audit()
                   .WithAction(ActionType.Update)
                   .WithEventIdentifier(EventIdentifierType.Import)
                   .WithOutcome(OutcomeIndicator.Success)
                   .WithEventType("MoveQueueObject")
                   .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                   .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.Archiving, new Uri($"urn:santedb:org:DispatcherQueueInfo/{dqe.SourceQueue}/entry/{dqe.CorrelationId}"))
                   .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.Creation, new Uri($"urn:santedb:org:DispatcherQueueInfo/{scopingKey}/entry/{retVal.CorrelationId}"))
                   .WithLocalDestination()
                   .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
                   .WithPrincipal()
                   .Send();
                return retVal;
            }
            else
            {
                throw new ArgumentException(nameof(item), "Expected DispatcherQueueEntry body");
            }
        }

        /// <summary>
        /// Get the specified queue object
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            return this.m_queueService.GetQueueEntry((string)scopingKey, (string)key);
        }

        /// <summary>
        /// Query for all entries on the specified queue
        /// </summary>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var entries = this.m_queueService.GetQueueEntries((string)scopingKey);
            if (filter.TryGetValue("name", out var values))
            {
                entries = entries.Where(o => o.CorrelationId.Contains(values.First().Replace("*", "")));
            }

            _AuditService.Audit()
               .WithLocalDestination()
                   .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
               .WithPrincipal()
               .WithAction(ActionType.Execute)
               .WithEventIdentifier(EventIdentifierType.Query)
               .WithOutcome(OutcomeIndicator.Success)
               .WithTimestamp(DateTimeOffset.Now)
               .WithEventType("QueryQueueObject")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
               .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.PermanentErasure, entries.Select(o => new Uri($"urn:santedb:org:DispatcherQueueInfo/{scopingKey}/entry/{o.CorrelationId}")).ToArray())
               .Send();

            return new MemoryQueryResultSet(entries);
        }

        /// <summary>
        /// Remove the specified object
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            if (key == null || key.Equals("*"))
            {
                this.m_queueService.Purge((String)scopingKey);
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
                    .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.Resource, Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, new Uri($"urn:santedb:org:DispatcherQueueInfo/{scopingKey}/event/*"))
                    .Send();
            }
            else
            {
                var data = this.m_queueService.DequeueById((String)scopingKey, (string)key);
                _AuditService.Audit()
                    .WithLocalDestination()
                   .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
                    .WithPrincipal()
                    .WithAction(Core.Model.Audit.ActionType.Delete)
                    .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                    .WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithEventType("PurgeQueueObject")
                    .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                    .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.Resource, Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, new Uri($"urn:santedb:org:DispatcherQueueInfo/{scopingKey}/event/{key}"))
                    .Send();
            }
            return null;
        }
    }
}
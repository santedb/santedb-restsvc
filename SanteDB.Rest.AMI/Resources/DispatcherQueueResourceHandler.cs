/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using RestSrvr;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents the primary queue resource handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DispatcherQueueResourceHandler : IApiResourceHandler, IChainedApiResourceHandler
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(DispatcherQueueResourceHandler));

        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        // Queue service
        private readonly IDispatcherQueueManagerService m_queueService;

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// DI constructor for persistent queue
        /// </summary>
        public DispatcherQueueResourceHandler(IDispatcherQueueManagerService queueService, ILocalizationService localization)
        {
            this.m_queueService = queueService;
            this.m_localizationService = localization;
        }

        /// <summary>
        /// Get the name of the resource
        /// </summary>
        public string ResourceName => nameof(DispatcherQueueInfo);

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type => typeof(DispatcherQueueInfo);

        /// <summary>
        /// Gets the scope of the resource
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of the object
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Update;

        /// <summary>
        /// Get all child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// Create not supported
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get a specific queue entry
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object Get(object id, object versionId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obsolete the specified queue - not supported
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object Delete(object key)
        {
            this.m_queueService.Purge((String)key);
            AuditUtil.SendAudit(new Core.Model.Audit.AuditEventData()
                .WithLocalDevice()
                .WithUser()
                .WithAction(Core.Model.Audit.ActionType.Delete)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                .WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                .WithTimestamp(DateTimeOffset.Now)
                .WithEventType("PurgeQueue")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.Resource, Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, new Uri($"urn:santedb:org:DispatcherQueueInfo/{key}/event")));
            return null;
        }

        /// <summary>
        /// Query for all
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
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
        public object Update(object data)
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
        /// Add a child resource
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(typeof(DispatcherQueueInfo), scopingEntityKey, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(DispatcherQueueInfo), scopingEntityKey, filter);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(typeof(DispatcherQueueInfo), scopingEntityKey, scopedItem);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntity == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(typeof(DispatcherQueueInfo), scopingEntity, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Try to get a chained resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues)]
        public bool TryGetChainedResource(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildResourceHandler childHandler)
        {
            var retVal = this.m_propertyProviders.TryGetValue(propertyName, out childHandler) &&
                childHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                childHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }
    }
}
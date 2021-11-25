using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents the primary queue resource handler
    /// </summary>
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
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Delete | ResourceCapabilityType.Get;

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
        public object Obsolete(object key)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
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
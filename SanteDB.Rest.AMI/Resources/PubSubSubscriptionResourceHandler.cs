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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
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
    /// Publish Subscribe Resource Handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class PubSubSubscriptionResourceHandler : IServiceImplementation, IApiResourceHandler, IOperationalApiResourceHandler
    {
        // Operations
        private ConcurrentDictionary<String, IApiChildOperation> m_operations = new ConcurrentDictionary<string, IApiChildOperation>();

        // The manager for the pub-sub service
        private IPubSubManagerService m_manager;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(PubSubSubscriptionDefinition));

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Creates a new pub-sub manager resource hander
        /// </summary>
        public PubSubSubscriptionResourceHandler(IPubSubManagerService manager, ILocalizationService localizationService)
        {
            this.m_manager = manager;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => typeof(PubSubSubscriptionDefinition).GetSerializationName();

        /// <summary>
        /// Gets the type this handles
        /// </summary>
        public Type Type => typeof(PubSubSubscriptionDefinition);

        /// <summary>
        /// Gets the scoped service
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities of this resource handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate
            | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <summary>
        /// Get service name
        /// </summary>
        public string ServiceName => "Pub Sub Subscription Resource Handler";

        /// <summary>
        /// Gets the operations for the resource handler
        /// </summary>
        public IEnumerable<IApiChildOperation> Operations => this.m_operations.Values;

        /// <summary>
        /// Creates a new object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is PubSubSubscriptionDefinition definition)
            {
                try
                {
                    // First , find the channel
                    var retVal = this.m_manager.RegisterSubscription(definition.ResourceType, definition.Name, definition.Description, definition.Event, definition.Filter.First(), definition.ChannelKey, definition.SupportContact, definition.NotBefore, definition.NotAfter);
                    if (definition.IsActive)
                    {
                        this.m_manager.ActivateSubscription(retVal.Key.Value, true);
                    }

                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating subscription - {0}", e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.creatingSubscription"), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("Payload must be of type PubSubSubscriptionDefinition");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.payloadMustBePubSubSubscription"));
            }
        }

        /// <summary>
        /// Gets the specified definition
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public object Get(object id, object versionId)
        {
            if (id is Guid uuid)
            {
                try
                {
                    return this.m_manager.GetSubscription(uuid);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching subscription {0} - {1}", id, e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.fetchingSubscription", new { param = uuid.ToString() }), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("ID must be a uuid");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.idMustBeUUID"));
            }
        }

        /// <summary>
        /// Deletes the specified subscription
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeletePubSubSubscription)]
        public object Delete(object key)
        {
            if (key is Guid uuid)
            {
                try
                {
                    return this.m_manager.RemoveSubscription(uuid);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error obsoleting / deleting subscription {0} - {1}", uuid, e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.obsoletingSubscription", new { param = uuid.ToString() }), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("ID must be a uuid");
                throw new ArgumentOutOfRangeException(this.m_localizationService.GetString("error.rest.ami.idMustBeUUID"));
            }
        }

        /// <summary>
        /// Find all subscriptions
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<PubSubSubscriptionDefinition>(queryParameters);
                return this.m_manager.FindSubscription(filter);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error querying subscriptions - {0}", e);
                throw new Exception(this.m_localizationService.GetString("error.rest.ami.subscriptionQuery"), e);
            }
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.EnablePubSubSubscription)]
        public object Update(object data)
        {
            if (data is PubSubSubscriptionDefinition definition)
            {
                try
                {
                    var retVal = this.m_manager.UpdateSubscription(definition.Key.Value, definition.Name, definition.Description, definition.Event, definition.Filter.First(), definition.SupportContact, definition.NotBefore, definition.NotAfter);
                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating subscription {0} - {1}", definition.Key, e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.updatingSubscription", new { param = definition.Key.ToString() }), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("Parameter must be of type PubSubSubscription");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.incorrectParameterType"));
            }
        }

        /// <inheritdoc />
        public void AddOperation(IApiChildOperation property)
        {
            this.m_operations.TryAdd(property.Name, property);
        }

        /// <inheritdoc/>
        public object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters)
        {
            if (this.TryGetOperation(operationName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildOperation operation))
            {
                return operation.Invoke(typeof(PubSubSubscriptionDefinition), scopingEntityKey, parameters);
            }
            else
            {
                throw new KeyNotFoundException($"{operationName} not registered");
            }
        }

        /// <inheritdoc/>
        public bool TryGetOperation(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler)
        {
            return this.m_operations.TryGetValue(propertyName, out operationHandler);
        }
    }
}
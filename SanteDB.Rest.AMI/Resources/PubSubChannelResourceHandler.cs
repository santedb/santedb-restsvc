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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which can interact with the IPubSubManager interface.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class PubSubChannelResourceHandler : IServiceImplementation, IApiResourceHandler
    {
        // The manager for the pub-sub service
        private IPubSubManagerService m_manager;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(PubSubSubscriptionDefinition));

        private ILocalizationService m_localizationService;

        /// <summary>
        /// Creates a new pub-sub manager resource hander
        /// </summary>
        public PubSubChannelResourceHandler(IPubSubManagerService manager, ILocalizationService localizationService)
        {
            this.m_manager = manager;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => typeof(PubSubChannelDefinition).GetSerializationName();

        /// <summary>
        /// Gets the type this handles
        /// </summary>
        public Type Type => typeof(PubSubChannelDefinition);

        /// <summary>
        /// Gets the scoped service
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities of this service
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create |
            ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get |
            ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <summary>
        /// Get service name
        /// </summary>
        public string ServiceName => "Pub Sub Channel Resource Handler";

        /// <summary>
        /// Create the specified channel
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is PubSubChannelDefinition definition)
            {
                try
                {
                    PubSubChannelDefinition retVal = this.m_manager.RegisterChannel(definition.Name, definition.DispatcherFactoryId, new Uri(definition.Endpoint), definition.Settings?.ToDictionary(o => o.Name, o => o.Value));

                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating channel - {0}", e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.errorCreatingChannel"), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("Body must be of type PubSubChannelDefinition");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.bodyMustBePubSubChannel"));
            }
        }

        /// <summary>
        /// Get the specified channel
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public object Get(object id, object versionId)
        {
            if (id is Guid uuid)
            {
                return this.m_manager.GetChannel(uuid);
            }
            else
            {
                this.m_tracer.TraceError($"{id} is not a valid UUID");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.invalidUUID", new { param = id.ToString() }));
            }
        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeletePubSubSubscription)]
        public object Delete(object key)
        {
            if (key is Guid uuid)
            {
                return this.m_manager.RemoveChannel(uuid);
            }
            else
            {
                this.m_tracer.TraceError($"{key} is not a valid UUID");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.invalidUUID", new { param = key.ToString() }));
            }
        }

        /// <summary>
        /// Query the specified channels
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<PubSubChannelDefinition>(queryParameters);
                return this.m_manager.FindChannel(filter);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error querying channel definitions - {0}", e);
                throw new Exception(this.m_localizationService.GetString("error.rest.ami.errorPerformingChannelQuery"), e);
            }
        }

        /// <summary>
        /// Update the specified channel definition
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreatePubSubSubscription)]
        public object Update(object data)
        {
            if (data is PubSubChannelDefinition definition)
            {
                try
                {
                    return this.m_manager.UpdateChannel(definition.Key.Value, definition.Name, new Uri(definition.Endpoint), definition.Settings.ToDictionary(o => o.Name, o => o.Value));
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError($"Error updating channel definition: {definition.Key}", e);
                    throw new Exception(this.m_localizationService.GetString("error.rest.ami.updatingChannel", new { param = definition.Key }), e);
                }
            }
            else
            {
                this.m_tracer.TraceError("Body must be of type PubSubChannelDefinition");
                throw new ArgumentException(this.m_localizationService.GetString("error.rest.ami.bodyMustBePubSubChannel"));
            }
        }
    }
}
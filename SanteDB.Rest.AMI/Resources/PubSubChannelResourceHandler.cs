using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which can interact with the IPubSubManager interface.
    /// </summary>
    public class PubSubChannelResourceHandler : IApiResourceHandler
    {

        // The manager for the pub-sub service
        private IPubSubManagerService m_manager;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(PubSubSubscriptionDefinition));

        /// <summary>
        /// Creates a new pub-sub manager resource hander
        /// </summary>
        public PubSubChannelResourceHandler(IPubSubManagerService manager)
        {
            this.m_manager = manager;
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => "PubSubChannel";

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
        /// Create the specified channel
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            if (data is PubSubChannelDefinition definition)
            {
                try
                {
                    PubSubChannelDefinition retVal = null;
                    if (definition.DispatcherFactoryType != null)
                        retVal = this.m_manager.RegisterChannel(definition.Name, definition.DispatcherFactoryType, definition.Endpoint, definition.Settings.ToDictionary(o => o.Name, o => o.Value));
                    else
                        retVal = this.m_manager.RegisterChannel(definition.Name, definition.Endpoint, definition.Settings.ToDictionary(o => o.Name, o => o.Value));

                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating channel - {0}", e);
                    throw new Exception($"Error creating channel", e);
                }
            }
            else
                throw new ArgumentException("Body must be of type PubSubChannelDefinition");
        }

        /// <summary>
        /// Get the specified channel
        /// </summary>
        public object Get(object id, object versionId)
        {
            if (id is Guid uuid)
                return this.m_manager.GetChannel(uuid);
            else
                throw new ArgumentException($"{id} is not a valid UUID");
        }

        /// <summary>
        /// Obsolete the specifed object
        /// </summary>
        public object Obsolete(object key)
        {
            if (key is Guid uuid)
                return this.m_manager.RemoveChannel(uuid);
            else
                throw new ArgumentException($"{key} is not a valid UUID");
        }

        /// <summary>
        /// Query the specified channels
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return this.Query(queryParameters, 0, 20, out int _);
        }

        /// <summary>
        /// Query the specifed channels
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<PubSubChannelDefinition>(queryParameters);
                return this.m_manager.FindChannel(filter, offset, count, out totalCount);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error querying channel definitions - {0}", e);
                throw new Exception($"Error performing query for channel definitions", e);
            }
        }

        /// <summary>
        /// Update the specified channel definition
        /// </summary>
        public object Update(object data)
        {
            if (data is PubSubChannelDefinition definition)
            {
                try
                {
                    return this.m_manager.UpdateChannel(definition.Key.Value, definition.Name, definition.Endpoint, definition.Settings.ToDictionary(o => o.Name, o => o.Value));
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating channel definition", e);
                    throw new Exception($"Error updating channel {definition.Key}", e);
                }
            }
            else
                throw new ArgumentException($"Body must be of type PubSubChannelDefinition");
        }
    }
}

using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.PubSub;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Gets all instances of IPubSubDispatcher
    /// </summary>
    public class PubSubChannelDispatcherType : IApiChildOperation
    {
        // Dipstachers
        private String[] m_dispatchers;

        // Service manager
        private readonly IServiceManager m_serviceManager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PubSubChannelDispatcherType(IServiceManager serviceManager)
        {
            this.m_serviceManager = serviceManager;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(PubSubSubscriptionDefinition) };

        /// <inheritdoc/>
        public string Name => "dispatcher";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (this.m_dispatchers == null)
            {
                this.m_dispatchers = AppDomain.CurrentDomain.GetAllTypes()
                    .Where(t => typeof(IPubSubDispatcherFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .Select(o => this.m_serviceManager.CreateInjected(o) as IPubSubDispatcherFactory)
                    .SelectMany(o => o.Schemes)
                    .ToArray();
            }
            return new GenericRestResultCollection() { Values = this.m_dispatchers.OfType<Object>().ToList() };
        }
    }
}
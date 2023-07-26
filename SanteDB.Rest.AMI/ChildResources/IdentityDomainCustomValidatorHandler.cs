using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Types;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// A sub-resource handler which gets check digit handlers
    /// </summary>
    public class IdentityDomainCustomValidatorHandler : IApiChildResourceHandler
    {
        private readonly IServiceManager m_serviceManager;

        /// <summary>
        /// DI CTOR
        /// </summary>
        public IdentityDomainCustomValidatorHandler(IServiceManager serviceManager)
        {
            this.m_serviceManager = serviceManager;
        }

        /// <inheritdoc/>
        public string Name => "_validator";

        /// <inheritdoc/>
        public Type PropertyType => typeof(AmiTypeDescriptor);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IdentityDomain) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            return this.m_serviceManager.CreateInjectedOfAll<IIdentifierValidator>().Select(o => new AmiTypeDescriptor(o.GetType(), o.Name)).AsResultSet();
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}

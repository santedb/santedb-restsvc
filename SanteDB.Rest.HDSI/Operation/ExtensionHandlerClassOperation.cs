using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Extension handler class fetch function
    /// </summary>
    /// <remarks>This is an operation rather than a sub-resource as it returns a parameter collection</remarks>
    public class ExtensionHandlerClassOperation : IApiChildOperation
    {

        private readonly IExtensionHandler[] m_extensionHandlers;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExtensionHandlerClassOperation()
        {
            this.m_extensionHandlers = AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IExtensionHandler).IsAssignableFrom(t))
                .Select(t =>
                {
                    try
                    {
                        return Activator.CreateInstance(t) as IExtensionHandler;
                    }
                    catch
                    {
                        return null;
                    }
                }).OfType<IExtensionHandler>().ToArray();
        }
        /// <inheritdoc/>
        public string Name => "handlers";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(ExtensionType) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            return new ParameterCollection(this.m_extensionHandlers.Select(h => new Parameter(h.Name, h.GetType().AssemblyQualifiedNameWithoutVersion())).ToArray());
        }
    }
}

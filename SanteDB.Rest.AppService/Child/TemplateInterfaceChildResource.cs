using RestSrvr;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace SanteDB.Rest.AppService.Child
{
    /// <summary>
    /// Child resource handler which renders out the forms
    /// </summary>
    public class TemplateInterfaceChildResource : IApiChildResourceHandler
    {

        private IAppletManagerService m_appletManager;

        /// <summary>
        /// DI Ctor
        /// </summary>
        public TemplateInterfaceChildResource(IAppletManagerService appletManager)
        {
            this.m_appletManager = appletManager;
        }

        /// <summary>
        /// Gets the scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Gets the parnt types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(TemplateDefinition) };

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string Name => "ui";

        /// <summary>
        /// Gets the property type
        /// </summary>
        public Type PropertyType => typeof(Stream);

        /// <summary>
        /// Get the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var template = this.m_appletManager.Applets.GetTemplateDefinition(scopingKey.ToString());
            if (template == null)
                throw new KeyNotFoundException($"Template {scopingKey} not found");

            switch (key.ToString().ToLowerInvariant()) {
                case "view.html":
                    RestOperationContext.Current.OutgoingResponse.Redirect(template.View);
                    break;
                case "form.html":
                    RestOperationContext.Current.OutgoingResponse.Redirect(template.Form);
                    break;
            }
            return null;
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}

using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Operation
{
    /// <summary>
    /// Operation to disable a service
    /// </summary>
    public class DisableServiceOperation : IApiChildOperation
    {
        private readonly IServiceManager m_serviceManager;
        private readonly IConfigurationManager m_configurationManager;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DisableServiceOperation));

        /// <summary>
        /// Creates a new service operation
        /// </summary>
        public DisableServiceOperation(IServiceManager serviceManager, IConfigurationManager configurationManager)
        {
            this.m_serviceManager = serviceManager;
            this.m_configurationManager = configurationManager;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Get the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(ConfigurationViewModel) };

        /// <summary>
        /// The name of the operation
        /// </summary>
        public string Name => "disable-service";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(parameters.TryGet("service-name", out string serviceName))
            {
                try
                {
                    var svc = this.m_serviceManager.GetServices().FirstOrDefault(o => o.GetType().FullName == serviceName);
                    var svcType = svc?.GetType() ?? Type.GetType(serviceName);
                    if(svcType == null)
                    {
                        throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.SERVICE_NOT_FOUND, serviceName));
                    }

                    var serviceInstance = ApplicationServiceContext.Current.GetService(svcType);
                    if(serviceInstance is IDaemonService dmn)
                    {
                        dmn.Stop();
                    }

                    this.m_serviceManager.RemoveServiceProvider(svcType);
                    if (!this.m_configurationManager.IsReadonly)
                    {
                        this.m_configurationManager.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(o => o.Type == svcType);
                        this.m_configurationManager.SaveConfiguration();
                    }
                    return true;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error disabling service : {0}", e);
                    throw new Exception($"Could not disable service {serviceName}", e);
                }
            }
            else
            {
                throw new ArgumentException("service-name");
            }
        }
    }
}

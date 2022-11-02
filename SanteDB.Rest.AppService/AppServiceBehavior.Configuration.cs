using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior functions for configuration
    /// </summary>
    public partial class AppServiceBehavior
    {
        /// <inheritdoc/>
        public List<DiagnosticServiceInfo> GetServices()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void DisableService(string serviceType)
        {
            try
            {
                var svc = this.m_serviceManager.GetServices().FirstOrDefault(o => o.GetType().FullName == serviceType);
                var svcType = svc?.GetType() ?? Type.GetType(serviceType);
                if (svcType == null)
                {
                    throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.SERVICE_NOT_FOUND, serviceType));
                }

                var serviceInstance = ApplicationServiceContext.Current.GetService(svcType);
                if (serviceInstance is IDaemonService dmn)
                {
                    dmn.Stop();
                }

                this.m_serviceManager.RemoveServiceProvider(svcType);
                if (!this.m_configurationManager.IsReadonly)
                {
                    this.m_configurationManager.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(o => o.Type == svcType);
                    this.m_configurationManager.SaveConfiguration(); // This should trigger a restart of the application context
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error disabling service : {0}", e);
                throw new Exception($"Could not disable service {serviceType}", e);
            }
        }

        /// <inheritdoc/>
        public void EnableService(string serviceType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public List<AppSettingKeyValuePair> GetAppSettings(string scope)
        {
            if(scope != AuthenticationContext.Current.Principal.Identity.Name)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction);
            }

            return this.m_userPreferenceManager?.GetUserSettings(scope);
        }

        /// <inheritdoc/>
        public ConfigurationViewModel GetConfiguration()
        {
            return new ConfigurationViewModel(this.m_configurationFeatures);
        }

        /// <inheritdoc/>
        public List<StorageProviderViewModel> GetDataStorageProviders() => DataConfigurationSection.GetDataConfigurationProviders().Select(o => new StorageProviderViewModel(o)).ToList();

        /// <inheritdoc/>
        public void SetAppSettings(string scope, List<AppSettingKeyValuePair> settings)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public ConfigurationViewModel UpdateConfiguration(ConfigurationViewModel configuration)
        {
            throw new NotImplementedException();
        }
    }
}

using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Client.Configuration;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
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
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
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
            // If we're not configured then we don't need to demand - if we are configured - we need to get some demands
            if (this.m_upstreamManagementService.IsConfigured() ||
                !(this.m_configurationManager is InitialConfigurationManager))
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.Login);
                return new ConfigurationViewModel(this.m_configurationFeatures.Where(o => this.m_policyEnforcementService.SoftDemand(o.ReadPolicy, AuthenticationContext.Current.Principal)));
            }
            else
            {
                return new ConfigurationViewModel(this.m_configurationFeatures);
            }
        }

        /// <inheritdoc/>
        public List<StorageProviderViewModel> GetDataStorageProviders() => DataConfigurationSection.GetDataConfigurationProviders().Select(o => new StorageProviderViewModel(o)).ToList();

        /// <inheritdoc/>
        public void SetAppSettings(string scope, List<AppSettingKeyValuePair> settings)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction)]
        public ConfigurationViewModel UpdateConfiguration(ConfigurationViewModel configuration)
        {
            // Run through the configuration model and configure the objects
            try
            {
                var currentConfiguration = this.m_configurationManager.Configuration;
                foreach(var configHandler in this.m_configurationFeatures.OrderBy(o=>o.Order))
                {
                    if(configuration.Configuration.TryGetValue(configHandler.Name, out var settings))
                    {
                        this.m_policyEnforcementService.Demand(configHandler.WritePolicy);
                        if(!configHandler.Configure(currentConfiguration, settings))
                        {
                            throw new ConfigurationException($"Error applying configuration {configHandler.Name}", currentConfiguration);
                        }
                    }
                }

                if (this.m_configurationManager is IRequestRestarts irr)
                {
                    irr.RestartRequested += (o,e) => configuration.AutoRestart = true;
                }
                this.m_configurationManager.SaveConfiguration();
                return configuration;
            }
            catch(Exception e)
            {
                throw new FaultException(System.Net.HttpStatusCode.InternalServerError, "Error saving configuration", e);
            }
        }

    }
}

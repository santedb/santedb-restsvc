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
 */
using RestSrvr.Exceptions;
using SanteDB.Client.Configuration;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (scope.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                return this.m_userPreferenceManager?.GetUserSettings(AuthenticationContext.Current.Principal.Identity.Name).ToList();
            }
            else if (scope != AuthenticationContext.Current.Principal.Identity.Name)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AlterIdentity);
            }

            return this.m_userPreferenceManager?.GetUserSettings(scope).ToList();
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
        public List<String> GetIntegrationPatterns() => this.m_integrationPatterns.Select(o => o.Name).ToList();

        /// <inheritdoc/>
        public List<StorageProviderViewModel> GetDataStorageProviders() => DataConfigurationSection.GetDataConfigurationProviders().Where(o => o.HostType.HasFlag(ApplicationServiceContext.Current.HostType)).Select(o => new StorageProviderViewModel(o)).ToList();

        /// <inheritdoc/>
        public void SetAppSettings(string scope, List<AppSettingKeyValuePair> settings)
        {
            if (scope.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                this.m_userPreferenceManager?.SetUserSettings(AuthenticationContext.Current.Principal.Identity.Name, settings);
            }
            else if (scope != AuthenticationContext.Current.Principal.Identity.Name)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);
            }

        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public ConfigurationViewModel UpdateConfiguration(ConfigurationViewModel configuration)
        {
            // Run through the configuration model and configure the objects
            try
            {
                var currentConfiguration = this.m_configurationManager.Configuration;
                foreach (var configHandler in this.m_configurationFeatures.OrderBy(o => o.Order))
                {
                    if (configuration.Configuration.TryGetValue(configHandler.Name, out var settings))
                    {
                        this.m_policyEnforcementService.Demand(configHandler.WritePolicy);
                        if (!configHandler.Configure(currentConfiguration, settings))
                        {
                            throw new ConfigurationException($"Error applying configuration {configHandler.Name}", currentConfiguration);
                        }
                    }
                }

                if (this.m_configurationManager is IRequestRestarts irr)
                {
                    irr.RestartRequested += (o, e) => configuration.AutoRestart = true;
                }
                this.m_configurationManager.SaveConfiguration();
                return configuration;
            }
            catch (Exception e)
            {
                throw new FaultException(System.Net.HttpStatusCode.InternalServerError, "Error saving configuration", e);
            }
        }

    }
}

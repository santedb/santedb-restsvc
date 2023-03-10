/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using RestSrvr;
using RestSrvr.Bindings;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Security;
using System;
using System.Linq;
using System.Net;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Rest service tool to create rest services
    /// </summary>
    public class RestServiceFactory : IRestServiceFactory
    {
        // Configuration
        private SanteDB.Rest.Common.Configuration.RestConfigurationSection m_configuration;

        // Service manager instance
        private IServiceManager m_serviceManager;

        /// <summary>
        /// Rest service factory
        /// </summary>
        public RestServiceFactory(IConfigurationManager configurationManager, IServiceManager serviceManager)
        {
            // Register a remote endpoint util that uses the RestOperationContext
            RemoteEndpointUtil.Current.AddEndpointProvider(this.GetRemoteEndpointInfo);
            this.m_configuration = configurationManager.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            this.m_serviceManager = serviceManager;
        }

        /// <summary>
        /// Retrieve the remote endpoint information
        /// </summary>
        /// <returns></returns>
        public RemoteEndpointInfo GetRemoteEndpointInfo()
        {
            if (RestOperationContext.Current == null)
            {
                return null;
            }
            else
            {
                var fwdHeader = RestOperationContext.Current?.IncomingRequest.Headers["X-Forwarded-For"];
                var realIp = RestOperationContext.Current.IncomingRequest.Headers["X-Real-IP"];
                var requestUrl = RestOperationContext.Current?.IncomingRequest.Url;
                return new RemoteEndpointInfo()
                {
                    OriginalRequestUrl = requestUrl.ToString(),
                    RemoteAddress = realIp ?? RestOperationContext.Current?.IncomingRequest.RemoteEndPoint.Address.ToString(),
                    ForwardInformation = fwdHeader,
                    CorrelationToken = RestOperationContext.Current?.Data["uuid"]?.ToString()
                };
            }
        }

        /// <summary>
        /// Get capabilities
        /// </summary>
        public int GetServiceCapabilities(RestService me)
        {
            var retVal = ServiceEndpointCapabilities.None;
            // Any of the capabilities are for security?
            if (me.ServiceBehaviors.OfType<SanteDB.Rest.Common.Security.TokenAuthorizationAccessBehavior>().Any())
            {
                retVal |= ServiceEndpointCapabilities.BearerAuth;
            }
            if(me.ServiceBehaviors.OfType<ClientCertificateAccessBehavior>().Any())
            {
                retVal |= ServiceEndpointCapabilities.CertificateAuth;
            }

            if (me.ServiceBehaviors.OfType<BasicAuthorizationAccessBehavior>().Any())
            {
                retVal |= ServiceEndpointCapabilities.BasicAuth;
            }

            if (me.Endpoints.Any(e => e.Behaviors.OfType<MessageCompressionEndpointBehavior>().Any()))
            {
                retVal |= ServiceEndpointCapabilities.Compression;
            }

            if (me.Endpoints.Any(e => e.Behaviors.OfType<CorsEndpointBehavior>().Any()))
            {
                retVal |= ServiceEndpointCapabilities.Cors;
            }

            if (me.Endpoints.Any(e => e.Behaviors.OfType<MessageDispatchFormatterBehavior>().Any()))
            {
                retVal |= ServiceEndpointCapabilities.ViewModel;
            }

            return (int)retVal;
        }

        /// <summary>
        /// Create the rest service
        /// </summary>
        public RestService CreateService(String serviceConfigurationName)
        {
            try
            {
                // Get the configuration
                var config = this.m_configuration.Services.FirstOrDefault(o => o.ConfigurationName == serviceConfigurationName);
                if (config == null)
                {
                    throw new InvalidOperationException($"Cannot find configuration for {serviceConfigurationName}");
                }

                RestService retVal = new RestService(config.ServiceType);
                foreach (var bhvr in config.Behaviors)
                {
                    if (bhvr.Type == null)
                    {
                        throw new InvalidOperationException($"Cannot find service behavior {bhvr.XmlType}");
                    }

                    retVal.AddServiceBehavior(
                        bhvr.Configuration == null ?
                        Activator.CreateInstance(bhvr.Type) as IServiceBehavior :
                        Activator.CreateInstance(bhvr.Type, bhvr.Configuration) as IServiceBehavior);
                }
                var demandPolicy = new OperationDemandPolicyBehavior(config.ServiceType);

                foreach (var ep in config.Endpoints)
                {
                    var se = retVal.AddServiceEndpoint(new Uri(ep.Address), ep.Contract, new RestHttpBinding());
                    se.AddEndpointBehavior(demandPolicy);
                    foreach (var bhvr in ep.Behaviors)
                    {
                        if (bhvr.Type == null)
                        {
                            throw new InvalidOperationException($"Cannot find endpoint behavior {bhvr.XmlType}");
                        }

                        se.AddEndpointBehavior(
                            bhvr.Configuration == null ?
                            Activator.CreateInstance(bhvr.Type) as IEndpointBehavior :
                            Activator.CreateInstance(bhvr.Type, bhvr.Configuration) as IEndpointBehavior);
                    }
                }
                return retVal;
            }
            catch (Exception e)
            {
                if (e is InvalidOperationException && e.Message.Contains("configuration") && m_configuration?.Services?.All(c => c.ConfigurationName == null) == true)
                {
                    Tracer.GetTracer(typeof(RestServiceFactory)).TraceError("Could not start {0} : {1}. This may be caused by not having name attributes on service elements in the configuration.", serviceConfigurationName, e);
                }
                else
                {
                    Tracer.GetTracer(typeof(RestServiceFactory)).TraceError("Could not start {0} : {1}", serviceConfigurationName, e);
                }
                throw new Exception($"Could not start {serviceConfigurationName}", e);
            }
        }
    }
}
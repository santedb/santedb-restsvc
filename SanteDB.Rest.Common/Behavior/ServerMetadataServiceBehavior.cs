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
 * User: fyfej
 * Date: 2023-6-21
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using System.Reflection;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// Server metadata behavior adds server information to the outbound http headers
    /// </summary>
    public class ServerMetadataServiceBehavior : IEndpointBehavior, IMessageInspector
    {
        /// <inheritdoc cref="IMessageInspector.AfterReceiveRequest(RestRequestMessage)"/>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
        }

        /// <inheritdoc cref="IEndpointBehavior.ApplyEndpointBehavior(ServiceEndpoint, EndpointDispatcher)"/>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <inheritdoc cref="IMessageInspector.BeforeSendResponse(RestResponseMessage)"/>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyname = assembly?.GetName();


            response.Headers.Add("X-SanteDB-Application", ApplicationServiceContext.Current.ApplicationName);
            response.Headers.Add("X-Powered-By", string.Format("{0} v{1} ({2})", assemblyname?.Name, assemblyname?.Version, assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));
        }
    }
}

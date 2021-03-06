﻿/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// CORS settings
    /// </summary>
    [XmlRoot(nameof(ServiceThrottlingConfiguration), Namespace = "http://santedb.org/configuration")]
    [XmlType(nameof(ServiceThrottlingConfiguration), Namespace = "http://santedb.org/configuration")]
    public class ServiceThrottlingConfiguration
    {
       
        /// <summary>
        /// Gets the resource settings
        /// </summary>
        [XmlElement("limit")]
        public Int32 Limit { get; set; }

    }
    
    /// <summary>
    /// Represents a basic service throttling behavior that limits the concurrent number of 
    /// requests on a particular endpoint.
    /// </summary>
    [Guid("B54FAA80-AA62-4069-B4A6-9AE970E3B222")]
    public class ServiceThrottlingEndpointBehavior : IEndpointBehavior, IMessageInspector, IDiagnosticsProbe
    {
        // Current request count
        private long m_requests = 0;

        // Settings
        private ServiceThrottlingConfiguration m_settings;

        /// <summary>
        /// CORS endpoint behavior as configured from endpoint behavior
        /// </summary>
        public ServiceThrottlingEndpointBehavior(XElement xe) 
        {
            if (xe == null)
                throw new InvalidOperationException("Missing ServiceThrottlingConfiguration");
            using (var sr = new StringReader(xe.ToString()))
                this.m_settings = XmlModelSerializerFactory.Current.CreateSerializer(typeof(ServiceThrottlingConfiguration)).Deserialize(sr) as ServiceThrottlingConfiguration;
        }

        /// <summary>
        /// Creates a new CORS endpoint behavior
        /// </summary>
        public ServiceThrottlingEndpointBehavior(ServiceThrottlingConfiguration settings) 
        {
            this.m_settings = settings;
        }

        /// <summary>
        /// Gets the uuid of this diagnostics probe
        /// </summary>
        public Guid Uuid => this.GetType().GUID;

        /// <summary>
        /// Gets the name of the endpoint behavior
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the description of this object
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the value of the probe
        /// </summary>
        public object Value => (float)this.m_requests / (float)this.m_settings.Limit;

        /// <summary>
        /// Gets the type of the probe
        /// </summary>
        public Type Type => typeof(float);

        /// <summary>
        /// After receiving a request
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {

            var cReq = Interlocked.Increment(ref this.m_requests);
            if (cReq > this.m_settings.Limit)
                throw new LimitExceededException();

        }

        /// <summary>
        /// Apply endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
            this.Name = $"Endpoint throttling ({endpoint.Description.ListenUri})";
            this.Description = $"Shows % of total throttling for {endpoint.Description.ListenUri}";
        }

        /// <summary>
        /// Before sending a response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            // Decrement our instance
            Interlocked.Decrement(ref this.m_requests);
        }
       
    }
}

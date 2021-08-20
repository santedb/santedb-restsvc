/*
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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Serialization;
using SanteDB.Rest.Common.Serialization;
using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// Adds message CORS insepectors
    /// </summary>
    [DisplayName("Cross-Origin-Scripting (CORS) Support")]
    public class CorsEndpointBehavior : IEndpointBehavior
    {

        // Settings
        private CorsEndpointBehaviorConfiguration m_settings;

        /// <summary>
        /// CORS endpoint behavior as configured from endpoint behavior
        /// </summary>
        public CorsEndpointBehavior(XElement xe)
        {
            if (xe == null)
                throw new InvalidOperationException("Missing CorsEndpointBehaviorConfiguration");
            using (var sr = new StringReader(xe.ToString()))
                this.m_settings = XmlModelSerializerFactory.Current.CreateSerializer(typeof(CorsEndpointBehaviorConfiguration)).Deserialize(sr) as CorsEndpointBehaviorConfiguration;
        }
        /// <summary>
        /// Creates a new CORS endpoint behavior
        /// </summary>
        public CorsEndpointBehavior(CorsEndpointBehaviorConfiguration settings)
        {
            this.m_settings = settings;
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        public CorsEndpointBehavior()
        {
            this.m_settings = new CorsEndpointBehaviorConfiguration();
            this.m_settings.Resource.Add(new CorsResourceSetting("*", "*", new String[] { "GET", "POST", "HEAD", "PUT", "PATCH", "OPTIONS" }, new String[] { "Content-Type", "Accept-Encoding", "Content-Encoding" }));
        }

        /// <summary>
        /// Apply endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(new CorsMessageInspector(this.m_settings));
        }
    }
}

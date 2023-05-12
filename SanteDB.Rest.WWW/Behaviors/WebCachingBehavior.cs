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
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Rest.WWW.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SanteDB.Rest.WWW.Behaviors
{
    /// <summary>
    /// A service behavior that dictates browser caching controls
    /// </summary>
    public class WebCachingBehavior : IEndpointBehavior, IMessageInspector
    {
        // Extensions which may be cached
        private readonly string[] m_cacheExtensions;
        private readonly WwwConfigurationSection m_configuration;

        /// <summary>
        /// Default ctor with no configuration
        /// </summary>
        public WebCachingBehavior() : this(null)
        {

        }

        /// <summary>
        /// Ctor with configuration
        /// </summary>
        /// <remarks>Allows the configuraiton subsystem to control caching on specified resources</remarks>
        /// <param name="configurationElement">The configuration element</param>
        public WebCachingBehavior(XElement configurationElement)
        {
            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<WwwConfigurationSection>();

            if (configurationElement == null)
            {
                // Use global
                if (this.m_configuration?.CacheExtensions?.Any() == true)
                    this.m_cacheExtensions = this.m_configuration.CacheExtensions.ToArray();
                else
                {
                    this.m_cacheExtensions = new string[] { ".css", ".js", ".json", ".png", ".jpg", ".woff2", ".ttf" };
                }
            }
            else
            {
                this.m_cacheExtensions = configurationElement.Elements((XNamespace)"http://santedb.org/configuration" + "extension").Select(o => o.Value).ToArray();
            }

        }

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

            var extension = Path.GetExtension(RestOperationContext.Current.IncomingRequest.Url.AbsolutePath);
            if (this.m_cacheExtensions.Contains(extension))
            {
                response.Headers.Add("Cache-Control", $"public, max-age={this.m_configuration?.MaxAge ?? 120}, must-revalidate"); // TODO: make this configurable
                response.Headers.Add("Expires", DateTime.UtcNow.AddMinutes(this.m_configuration?.MaxAge ?? 120).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'"));
            }
            else
            {
                response.Headers.Add("Cache-Control", "no-cache");
            }

        }
    }
}

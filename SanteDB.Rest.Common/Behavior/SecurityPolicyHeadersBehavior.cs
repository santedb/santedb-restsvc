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
using RestSrvr.Message;
using SanteDB.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SanteDB.Rest.Common.Behaviors
{
    /// <summary>
    /// Implements the Content-Security-Policy header
    /// </summary>
    [DisplayName("Content-Security-Policy Header Support")]
    public class SecurityPolicyHeadersBehavior : IEndpointBehavior, IMessageInspector
    {

        /// <summary>
        /// Gets the NONCE for this instance of the policy behavior
        /// </summary>
        public String Nonce { get; }

        /// <summary>
        /// Policy behavior configuration
        /// </summary>
        public SecurityPolicyHeadersBehavior()
        {
            this.Nonce = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "");
        }

        /// <summary>
        /// After receiving request (not applicable)
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            ;
        }

        /// <summary>
        /// Applet the content security header
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <summary>
        /// Before sending a response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {

            //response.Headers.Add("Content-Security-Policy", $"script-src-elem 'nonce-{this.Nonce}'; script-src 'self'");
            response.Headers.Add("Content-Security-Policy", $"script-src-elem 'self' 'nonce-{this.Nonce}' 'strict-dynamic'; script-src 'self' 'nonce-{this.Nonce}'");
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            response.Headers.Add("X-Frame-Options", "deny");

            response.Headers.Add("Feature-Policy", "autoplay 'none'; accelerometer 'none'; geolocation 'none'; payment 'none'");

            response.Headers.Add("X-Content-Type-Options", "nosniff");
            
        }
    }
}

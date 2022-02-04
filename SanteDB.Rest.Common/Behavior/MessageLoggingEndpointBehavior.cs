﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// Represents an endpoint behavior that logs messages
    /// </summary>
    [DisplayName("Inbound/Outbound Message Logging Support")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class MessageLoggingEndpointBehavior : IEndpointBehavior, IMessageInspector
    {
        // Trace source name
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(MessageLoggingEndpointBehavior));

        // Correlation id
        [ThreadStatic]
        private static KeyValuePair<Guid, DateTime> httpCorrelation;

        /// <summary>
        /// After receiving the request
        /// </summary>
        /// <param name="request"></param>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            Guid httpCorrelator = Guid.NewGuid();

            this.m_traceSource.TraceEvent(EventLevel.Verbose, "HTTP RQO {0} : {1} {2} ({3}) - {4}",
                RestOperationContext.Current.IncomingRequest.RemoteEndPoint,
                request.Method,
                request.Url,
                RestOperationContext.Current.IncomingRequest.UserAgent,
                httpCorrelator);

            httpCorrelation = new KeyValuePair<Guid, DateTime>(httpCorrelator, DateTime.Now);
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <summary>
        /// Before sending the response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            var processingTime = DateTime.Now.Subtract(httpCorrelation.Value);

            this.m_traceSource.TraceEvent(EventLevel.Verbose, "HTTP RSP {0} : {1} ({2} ms)",
                httpCorrelation.Key,
                response.StatusCode,
                processingTime.TotalMilliseconds);
        }
    }
}
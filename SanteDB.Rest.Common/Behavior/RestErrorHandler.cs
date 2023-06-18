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
 * Date: 2023-5-19
 */
using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Http;
using SanteDB.Core.Security.Audit;
using SanteDB.Rest.Common.Fault;
using SanteDB.Rest.Common.Serialization;
using System;
using System.Linq;
using System.Net;

#pragma warning disable CS0612

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// Error handler
    /// </summary>
    public class RestErrorHandler : IServiceErrorHandler
    {
        // Error tracer
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(RestErrorHandler));

        /// <summary>
        /// Handle error
        /// </summary>
        public bool HandleError(Exception error)
        {
            return true;
        }

        /// <summary>
        /// Provide fault
        /// </summary>
        public bool ProvideFault(Exception error, RestResponseMessage faultMessage)
        {
            var uriMatched = RestOperationContext.Current.IncomingRequest.Url;

            // Root cause 
            var rootCause = error;
            while (rootCause.InnerException != null) rootCause = rootCause.InnerException;

            RestServiceFault fault = null;
            if (rootCause is DetectedIssueException dte) // Relay the detected issue first 
            {
                fault = new RestServiceFault(dte);
            }
            else if (rootCause is RestClientException<Object> rco && rco.Result is RestServiceFault rcf)
            {
                fault = rcf;
            }
            else
            {
                fault = new RestServiceFault(error);
            }

            faultMessage.StatusCode = error.GetHttpStatusCode();
            switch (faultMessage.StatusCode)
            {
                case HttpStatusCode.Conflict:
                case HttpStatusCode.ServiceUnavailable:
                    this.m_traceSource.TraceInfo("Issue on REST pipeline: {0}", error);
                    break;
                case HttpStatusCode.Unauthorized:
                    var authService = RestOperationContext.Current.AppliedPolicies.OfType<IAuthorizationServicePolicy>().FirstOrDefault();
                    authService.AddAuthenticateChallengeHeader(faultMessage, error);
                    break;
                case HttpStatusCode.NotModified:
                    return true;
                case (HttpStatusCode)429:
                    if (error is LimitExceededException lee)
                    {
                        faultMessage.Headers.Add("Retry-After", lee.RetryAfter.ToString());

                    }
                    else
                    {
                        faultMessage.Headers.Add("Retry-After", "3600");
                    }
                    break;
            }

            // Get the root cause / fault for the user 

            if (error is FaultException fe)
            {
                faultMessage.Headers.Add(fe.Headers);
            }

            if (RestOperationContext.Current.ServiceEndpoint != null)
            {

                RestMessageDispatchFormatter.CreateFormatter(RestOperationContext.Current.ServiceEndpoint.Description.Contract.Type).SerializeResponse(faultMessage, null, fault);
            }
            else
            {
                RestMessageDispatchFormatter.CreateFormatter(typeof(IRestApiContractImplementation)).SerializeResponse(faultMessage, null, fault);
            }

            if ((int)faultMessage.StatusCode >= 500)
            {
                this.m_traceSource.TraceWarning("Server Error on REST pipeline: {0}", error);
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(error, uriMatched, RestOperationContext.Current.IncomingRequest.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.IncomingRequest.Headers[o]), RestOperationContext.Current.OutgoingResponse.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.OutgoingResponse.Headers[o])).Send();

            }
            else if ((int)faultMessage.StatusCode >= 400)
            {
                this.m_traceSource.TraceWarning("Client Error on REST pipeline: {0}", error);
                ApplicationServiceContext.Current.GetAuditService().Audit().ForNetworkRequestFailure(error, uriMatched, RestOperationContext.Current.IncomingRequest.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.IncomingRequest.Headers[o]), RestOperationContext.Current.OutgoingResponse.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.OutgoingResponse.Headers[o])).Send();
            }
            return true;
        }
    }
}
#pragma warning restore
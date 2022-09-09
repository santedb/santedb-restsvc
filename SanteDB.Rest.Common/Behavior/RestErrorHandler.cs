/*
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
 * Date: 2022-5-30
 */
using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Security;
using SanteDB.Rest.Common.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Serialization;

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

            while (error.InnerException != null)
                error = error.InnerException;

            var fault = new RestServiceFault(error);
            var authScheme = RestOperationContext.Current.AppliedPolicies.OfType<BasicAuthorizationAccessBehavior>().Any() ? "Basic" : "Bearer";
            var authRealm = RestOperationContext.Current.IncomingRequest.Url.Host;
            // Formulate appropriate response
            switch (error)
            {
                case SecuritySessionException ses:
                    switch (ses.Type)
                    {
                        case SessionExceptionType.Expired:
                        case SessionExceptionType.NotYetValid:
                        case SessionExceptionType.NotEstablished:
                            faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                            faultMessage.AddAuthenticateHeader(authScheme, authRealm, error: "unauthorized");
                            break;

                        default:
                            faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                            break;
                    }
                    break;
                case DomainStateException dse:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable;
                    break;
                case ObjectLockedException lockException:
                    faultMessage.StatusCode = 423;
                    fault.Data.Add(lockException.LockedUser);
                    break;
                case PolicyViolationException pve:
                    if (pve.PolicyDecision == PolicyGrantType.Elevate)
                    {
                        // Ask the user to elevate themselves
                        faultMessage.StatusCode = 401;
                        faultMessage.AddAuthenticateHeader(authScheme, authRealm, "insufficient_scope", pve.PolicyId, error.Message);
                    }
                    else
                    {
                        faultMessage.StatusCode = 403;
                    }
                    break;
                case SecurityException se:
                case UnauthorizedAccessException uae:
                    faultMessage.StatusCode = (int)HttpStatusCode.Forbidden;
                    break;

                case LimitExceededException lee:
                    faultMessage.StatusCode = (int)(HttpStatusCode)429;
                    faultMessage.StatusDescription = "Too Many Requests";
                    faultMessage.Headers.Add("Retry-After", "1200");
                    break;
                case AuthenticationException ae:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    faultMessage.AddAuthenticateHeader(authScheme, authRealm, "invalid_token", description: error.Message);
                    break;
                
                case FaultException fe:
                    faultMessage.StatusCode = (int)fe.StatusCode;
                    break;
                case Newtonsoft.Json.JsonException je:
                case System.Xml.XmlException xe:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                    break;
                case DuplicateNameException dne:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Conflict;
                    break;
                case FileNotFoundException fnf:
                case KeyNotFoundException knf:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                    break;
                case DetectedIssueException die:
                    faultMessage.StatusCode = (int)(System.Net.HttpStatusCode)422;
                    break;
                case NotImplementedException nie:
                    faultMessage.StatusCode = (int)HttpStatusCode.NotImplemented;
                    break;
                case NotSupportedException nse:
                    faultMessage.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
                case PatchException pe:
                    faultMessage.StatusCode = (int)HttpStatusCode.Conflict;
                    break;
                default:
                    faultMessage.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                    break;
            }

            switch (faultMessage.StatusCode)
            {
                case 409:
                case 429:
                case 503:
                    this.m_traceSource.TraceInfo("Issue on REST pipeline: {0}", error);
                    break;
                case 401:
                case 403:
                case 501:
                case 405:
                    this.m_traceSource.TraceWarning("Warning on REST pipeline: {0}", error);
                    break;
                default:
                    this.m_traceSource.TraceError("Error on REST pipeline: {0}", error);
                    break;
            }

            RestMessageDispatchFormatter.CreateFormatter(RestOperationContext.Current.ServiceEndpoint.Description.Contract.Type).SerializeResponse(faultMessage, null, fault);
            AuditUtil.AuditNetworkRequestFailure(error, uriMatched, RestOperationContext.Current.IncomingRequest.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.IncomingRequest.Headers[o]), RestOperationContext.Current.OutgoingResponse.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.OutgoingResponse.Headers[o]));
            return true;
        }
    }
}
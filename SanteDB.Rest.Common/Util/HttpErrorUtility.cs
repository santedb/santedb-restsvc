/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Error utility for classifying errors
    /// </summary>
    public static class HttpErrorUtility
    {


        /// <summary>
        /// Get fault data
        /// </summary>
        public static RestServiceFault GetFaultData(this RestServiceFault rsf, String nameOfType)
        {
            var seek = rsf;
            while(seek != null && seek.Type != nameOfType)
            {
                seek = seek.CausedBy;
            }
            return seek;
        }

        /// <summary>
        /// Add a WWW authenticate behavior
        /// </summary>
        public static void AddWwwAuthenticateHeader(this IAuthorizationServicePolicy me, string challengeMode, Exception error, RestResponseMessage faultMessage)
        {
            // Map error codes to headers according to https://www.rfc-editor.org/rfc/rfc6750#section-3
            switch (error)
            {
                case PolicyViolationException pve:
                    faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "insufficient_scope", pve.PolicyId, pve.Message);
                    break;
                case RestClientException<RestServiceFault> rco: // came from an upstream
                    var pvd = rco.Result.GetFaultData(nameof(PolicyViolationException));
                    if(pvd != null)
                    {
                        faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "insufficient_scope", pvd.PolicyId, pvd.Message);
                        return;
                    }
                    // Did the server send anything of note?
                    var upstreamAuthHeader = rco.Response.Headers[System.Net.HttpResponseHeader.WwwAuthenticate];
                    if (!String.IsNullOrEmpty(upstreamAuthHeader))
                    {
                        var tokens = upstreamAuthHeader.Split(' ').Skip(1).Where(t => !t.StartsWith("realm")).ToArray();
                        faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host);
                    }
                    else
                    {
                        faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_request", description: error.Message);
                    }
                    break;
                case SecuritySessionException sse:
                    switch (sse.Type)
                    {
                        case SessionExceptionType.Scope:
                            faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_scope", description: sse.Message);
                            break;
                        default:
                            faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_token", description: sse.Message);
                            break;
                    }
                    break;
                default:
                    faultMessage.AddAuthenticateHeader(challengeMode, RestOperationContext.Current.IncomingRequest.Url.Host, "invalid_request", description: error.Message);
                    break;
            }
        }
        /// <summary>
        /// Get the HTTP status code for <paramref name="exception"/>
        /// </summary>
        /// <param name="exception">The exception for which the error code should be obtained</param>
        /// <returns>The HTTP status code</returns>
        public static HttpStatusCode GetHttpStatusCode(this Exception exception)
        {
            // We need the root cause of the exception
            var rootException = exception;
            while (rootException.InnerException != null)
            {
                rootException = rootException.InnerException;
            }

            switch (rootException)
            {
                case PreconditionFailedException pfe:
                    switch (RestOperationContext.Current?.IncomingRequest?.HttpMethod.ToLowerInvariant())
                    {
                        case "get":
                        case "head":
                            return HttpStatusCode.NotModified;
                        case "put":
                        case "patch":
                        case "post":
                            return HttpStatusCode.Conflict;
                        default:
                            return HttpStatusCode.PreconditionFailed;
                    }
                case SecuritySessionException ses:
                    switch (ses.Type)
                    {
                        case SessionExceptionType.Expired:
                        case SessionExceptionType.NotYetValid:
                        case SessionExceptionType.NotEstablished:
                            return System.Net.HttpStatusCode.Unauthorized;
                        default:
                            return System.Net.HttpStatusCode.Forbidden;
                    }
                case DomainStateException dse:
                    return System.Net.HttpStatusCode.ServiceUnavailable;
                case ObjectLockedException lockException:
                    return (HttpStatusCode)423;
                case PolicyViolationException pve:
                    if (pve.PolicyDecision == PolicyGrantType.Elevate)
                    {
                        // Ask the user to elevate themselves
                        return HttpStatusCode.Unauthorized;
                    }
                    else
                    {
                        return HttpStatusCode.Forbidden;
                    }
                case SecurityException se:
                case UnauthorizedAccessException uae:
                    return HttpStatusCode.Forbidden;
                case LimitExceededException lee:
                    return (HttpStatusCode)429;
                case AuthenticationException ae:
                    return System.Net.HttpStatusCode.Unauthorized;
                case FaultException fe:
                    return fe.StatusCode;
                case Newtonsoft.Json.JsonException je:
                case System.Xml.XmlException xe:
                    return System.Net.HttpStatusCode.BadRequest;
                case ArgumentException ae:
                    return HttpStatusCode.BadRequest;
                case DuplicateNameException dne:
                    return System.Net.HttpStatusCode.Conflict;
                case FileNotFoundException fnf:
                case KeyNotFoundException knf:
                    return System.Net.HttpStatusCode.NotFound;
                case DetectedIssueException die:
                    return (System.Net.HttpStatusCode)422;
                case NotImplementedException nie:
                    return HttpStatusCode.NotImplemented;
                case NotSupportedException nse:
                    return HttpStatusCode.MethodNotAllowed;
                case PatchException pe:
                    return HttpStatusCode.Conflict;
                case RestClientException<Object> rco:
                    return rco.HttpStatus;
                case MissingMemberException mme:
                case FormatException fe:
                    return HttpStatusCode.BadRequest;
                default:
                    return System.Net.HttpStatusCode.InternalServerError;
            }
        }

    }
}

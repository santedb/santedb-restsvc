﻿using RestSrvr;
using RestSrvr.Exceptions;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
                default:
                    return System.Net.HttpStatusCode.InternalServerError;
            }
        }

    }
}
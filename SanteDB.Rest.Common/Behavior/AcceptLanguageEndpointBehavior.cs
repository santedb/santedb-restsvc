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
 * Date: 2022-5-30
 */
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Rest.Common.Security;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// A service behavior that changes the current UI culture
    /// </summary>
    [DisplayName("Accept-Language Header Support")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class AcceptLanguageEndpointBehavior : IEndpointBehavior, IMessageInspector
    {
        // Trace
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AcceptLanguageEndpointBehavior));

        /// <summary>
        /// After receive a request look for the language
        /// </summary>
        /// <param name="request"></param>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            try
            {
                RestOperationContext.Current.Data.Add("originalLanguage", Thread.CurrentThread.CurrentUICulture.Name);
                var langPrincipal = AuthenticationContext.Current.Principal.GetClaimValue(SanteDBClaimTypes.Language);
                if (langPrincipal != null)
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(langPrincipal);
                else if (RestOperationContext.Current.Data.TryGetValue("Session", out object dataSession) && dataSession is ISession session &&
                    session.Claims.Any(o => o.Type == SanteDBClaimTypes.Language))
                {
                    langPrincipal = session.Claims.First(o => o.Type == SanteDBClaimTypes.Language)?.Value;
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(langPrincipal);
                }
                else if (request.Headers["Accept-Language"] != null)
                {
                    var language = request.Headers["Accept-Language"].Split(',');
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(language[0]);
                }

                if (request.Headers["X-SdbLanguage"] != null) // Language override
                {
                    var language = request.Headers["X-SdbLanguage"].Split(',');
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(language[0]);
                }
                RestOperationContext.Current.Data.Add("lang", Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceWarning("Error setting encoding: {0}", e.Message);
            }
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <summary>
        /// Send response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            try
            {
                response.Headers.Add("Content-Language", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                if (RestOperationContext.Current.Data.TryGetValue("originalLanguage", out Object name))
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(name.ToString());
            }
            catch (Exception e)
            {
                this.m_tracer.TraceWarning("Error setting culture - {0}", e);
            }
        }
    }
}
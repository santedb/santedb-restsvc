/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */

using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Session information resource handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SessionInfoResourceHandler : IApiResourceHandler, IServiceImplementation
    {
        // ILocalization Service
        private readonly ILocalizationService m_localizationService;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(SessionInfoResourceHandler));

        /// <summary>
        /// Instantiate the localization service
        /// </summary>
        /// <param name="localizationService"></param>
        public SessionInfoResourceHandler(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "SessionInfo";

        /// <summary>
        /// Gets the type that this resource handler returns
        /// </summary>
        public Type Type => typeof(SecuritySessionInfo);

        /// <summary>
        /// Gets the scoped object
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities for this object
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        /// <summary>
        /// Gets the service name
        /// </summary>

        public string ServiceName => "Session Inoformation Resource Service";

        /// <summary>
        /// Create an object
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException.userMessage"));
        }

        /// <summary>
        /// Gets the specified session object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Get(object id, object versionId)
        {
            var uuid = (Guid)id;
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(uuid.ToByteArray(), true);
            if (session == null)
            {
                this.m_tracer.TraceError($"Session {uuid} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.rest.ami.sessionNotFound", new
                {
                    param = uuid
                }));
            }
            return new SecuritySessionInfo(session);
        }

        /// <summary>
        /// Obsolete the specified session
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Obsolete(object key)
        {
            var uuid = (Guid)key;
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(uuid.ToByteArray(), false);
            if (session == null)
            {
                this.m_tracer.TraceError($"Session {uuid} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.rest.ami.sessionNotFound", new
                {
                    param = uuid
                }));
            }
            ApplicationServiceContext.Current.GetService<ISessionProviderService>().Abandon(session);
            return null;
        }

        /// <summary>
        /// Query for sessions
        /// </summary>
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException.userMessage"));
        }

        /// <summary>
        /// Update the specified session
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException.userMessage"));
        }
    }
}
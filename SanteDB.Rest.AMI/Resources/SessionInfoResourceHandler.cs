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
using System.Collections.Specialized;
using System.Linq;

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
        private readonly ISessionProviderService m_sessionProvider;
        private readonly IIdentityProviderService m_identityProvider;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(SessionInfoResourceHandler));

        /// <summary>
        /// Instantiate the localization service
        /// </summary>
        public SessionInfoResourceHandler(ILocalizationService localizationService, ISessionProviderService sessionProvider, IIdentityProviderService identityProviderService)
        {
            this.m_localizationService = localizationService;
            this.m_sessionProvider = sessionProvider;
            this.m_identityProvider = identityProviderService;
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

        public string ServiceName => "Session Information Resource Service";

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
            byte[] sessionId = null;
            if (id is Guid uuid)
            {
                sessionId = uuid.ToByteArray();
            }
            else if (id is String str)
            {
                try
                {
                    sessionId = str.HexDecode();
                }
                catch
                {
                    sessionId = Convert.FromBase64String(str);
                }
            }

            var session = this.m_sessionProvider.Get(sessionId, true);
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
        public object Delete(object key)
        {
            byte[] sessionId = null;
            if (key is Guid uuid)
            {
                sessionId = uuid.ToByteArray();
            }
            else if (key is String str)
            {
                try
                {
                    sessionId = str.HexDecode();
                }
                catch
                {
                    sessionId = Convert.FromBase64String(str);
                }
            }

            var session = this.m_sessionProvider.Get(sessionId, false);
            if (session == null)
            {
                this.m_tracer.TraceError($"Session {uuid} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.rest.ami.sessionNotFound", new
                {
                    param = uuid
                }));
            }
            this.m_sessionProvider.Abandon(session);
            return null;
        }

        /// <summary>
        /// Query for sessions
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            if (queryParameters["userIdentity"] != null)
            {
                var userId = this.m_identityProvider.GetSid(queryParameters["userIdentity"]);
                return this.m_sessionProvider.GetUserSessions(userId).Select(o => new SecuritySessionInfo(o)).AsResultSet();
            }
            else
            {
                return this.m_sessionProvider.GetActiveSessions().Select(o => new SecuritySessionInfo(o)).AsResultSet();
            }
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
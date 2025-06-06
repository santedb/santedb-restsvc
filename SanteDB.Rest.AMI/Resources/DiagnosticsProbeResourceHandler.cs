﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Performance counter handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DiagnosticsProbeResourceHandler : IServiceImplementation, IApiResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Probe";

        /// <summary>
        /// Gets the type that this handler returns
        /// </summary>
        public Type Type => typeof(DiagnosticsProbe);

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Diagnostics Probe Resource Handler";

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DiagnosticsProbeResourceHandler));

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Initializes the diagnostics probe resource handler
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        public DiagnosticsProbeResourceHandler(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Create an entity
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Gets the specified performance counter
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Get(object id, object versionId)
        {
            return new DiagnosticsProbeReading(DiagnosticsProbeManager.Current.Get((Guid)id));
        }

        /// <summary>
        /// Delete the probe
        /// </summary>
        public object Delete(object key)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Query for probes
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<IDiagnosticsProbe>(queryParameters);
                return new MemoryQueryResultSet(DiagnosticsProbeManager.Current.Find(filter.Compile()).Select(o => new DiagnosticsProbe((IDiagnosticsProbe)o)));
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError($"Error querying probes : {e.Message}");
                throw new Exception(this.m_localizationService.GetString("error.rest.ami.errorQueryingProbes", new { param = e.Message }), e);
            }
        }

        /// <summary>
        /// Update the object
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }
    }
}
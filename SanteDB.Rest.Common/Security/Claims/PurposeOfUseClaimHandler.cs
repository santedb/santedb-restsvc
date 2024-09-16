﻿/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;
using System;
using System.Diagnostics.Tracing;
using System.Security.Principal;

namespace SanteDB.Rest.Common.Security.Claims
{
    /// <summary>
    /// A claim handler which validates the purpose of use claim
    /// </summary>
    public class PurposeOfUseClaimHandler : IClaimTypeHandler
    {
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(PurposeOfUseClaimHandler));

        /// <summary>
        /// Gets the name of the claim being validated
        /// </summary>
        public string ClaimType
        {
            get
            {
                return SanteDBClaimTypes.PurposeOfUse;
            }
        }

        /// <summary>
        /// Validate the claim being made
        /// </summary>
        public bool Validate(IPrincipal principal, String value)
        {
            IConceptRepositoryService conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();

            try
            {
                // TODO: Validate that the "value" comes from the configured POU domain

                return true;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw;
            }
        }
    }
}
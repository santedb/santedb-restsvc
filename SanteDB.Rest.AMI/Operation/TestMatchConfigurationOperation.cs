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
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Matching;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Test the match configuration REST operation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class TestMatchConfigurationOperation : IApiChildOperation
    {
        // Config service
        private readonly IRecordMatchingConfigurationService m_configService;

        // Matching service
        private readonly IRecordMatchingService m_matchingService;

        // Match report factory
        private readonly IMatchReportFactory m_matchReportFactory;

        /// <summary>
        /// Create a new match configuration operation
        /// </summary>
        public TestMatchConfigurationOperation(IRecordMatchingConfigurationService configService = null, IRecordMatchingService matchingService = null, IMatchReportFactory matchReportFactory = null)
        {
            this.m_configService = configService;
            this.m_matchingService = matchingService;
            this.m_matchReportFactory = matchReportFactory;
        }

        /// <summary>
        /// Gets the type to bind to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(IRecordMatchingConfiguration) };

        /// <summary>
        /// Gets the property name
        /// </summary>
        public string Name => "test";

        /// <summary>
        /// Test the match configuration is an instance method
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet("input", out String inputIdString) && Guid.TryParse(inputIdString, out Guid inputId))
            {
                // Matching is run on system context

                // Load the configuration
                var config = this.m_configService?.GetConfiguration(scopingKey.ToString());
                if (config == null)
                {
                    throw new KeyNotFoundException($"{scopingKey} not found");
                }

                // Get the target input
                var inputObject = config.AppliesTo.Select(o =>
                {
                    using (AuthenticationContext.EnterSystemContext()) // The input should be in system concept since the privacy service may block our identity domains based on policy
                    {
                        var repo = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(o)) as IRepositoryService;
                        if (repo != null)
                        {
                            return repo.Get(inputId);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }).OfType<IdentifiedData>().FirstOrDefault();
                if (inputObject == null)
                {
                    throw new KeyNotFoundException($"{inputId} not found or not applicable for {scopingKey}");
                }

                // Run the configuration
                var mergeService = ApplicationServiceContext.Current.GetService(typeof(IRecordMergingService<>).MakeGenericType(inputObject.GetType())) as IRecordMergingService;
                var repoService = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(inputObject.GetType())) as IRepositoryService;

                IEnumerable<IdentifiedData> blocks = null;
                var diagnosticSession = this.m_matchingService.CreateDiagnosticSession();

                if (parameters.TryGet("targets", out String[] knownDuplicates) && knownDuplicates.Length > 0)
                {
                    // Start the "blocking" as specified
                    IEnumerable<IRecordMatchResult> results = null;
                    try
                    {
                        diagnosticSession.LogStart(config.Id);
                        blocks = knownDuplicates.Select(o => repoService.Get(Guid.Parse(o)));
                        results = this.m_matchingService.Classify(inputObject, blocks, config.Id, diagnosticSession);
                    }
                    finally
                    {
                        diagnosticSession.LogEnd();
                    }
                    return this.m_matchReportFactory.CreateMatchReport(inputObject.GetType(), inputObject, results, diagnosticSession);
                }
                else
                {
                    return this.m_matchReportFactory.CreateMatchReport(inputObject.GetType(), inputObject, this.m_matchingService.Match(inputObject, config.Id, mergeService.GetIgnoredKeys(inputId), diagnosticSession), diagnosticSession);
                }
            }
            else
            {
                throw new ArgumentNullException("Missing input patient parameter");
            }
        }
    }
}
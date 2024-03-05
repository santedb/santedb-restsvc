/*
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
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data.Management.Jobs;
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Linq;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Represents a match operation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class MatchOperation : IApiChildOperation
    {
        // Job manager
        private IJobManagerService m_jobManager;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class | ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Creates a new configuration match operation
        /// </summary>
        public MatchOperation(IConfigurationManager configurationManager, IJobManagerService jobManager)
        {
            this.m_jobManager = jobManager;
            var configuration = configurationManager.GetSection<ResourceManagementConfigurationSection>();
            this.ParentTypes = configuration?.ResourceTypes.Select(o => o.Type).ToArray() ?? Type.EmptyTypes;
        }

        /// <inheritdoc/>
        public Type[] ParentTypes { get; }

        /// <inheritdoc/>
        public string Name => "match";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            var merger = ApplicationServiceContext.Current.GetService(typeof(IRecordMergingService<>).MakeGenericType(scopingType)) as IRecordMergingService;
            if (merger == null)
            {
                throw new InvalidOperationException($"Cannot find merging service for {scopingType.Name}. Is it under matching control?");
            }

            if (scopingKey == null)
            {
                parameters.TryGet<bool>("clear", out bool clear);
                this.m_jobManager.StartJob(typeof(MatchJob<>).MakeGenericType(scopingType), new object[] { clear });
                return null;
            }
            else if (scopingKey is Guid scopingObjectKey)
            {

                // Now - we want to prepare a transaction
                Bundle retVal = new Bundle();

                if (parameters.TryGet<bool>("clear", out bool clear) && clear)
                {
                    merger.ClearMergeCandidates(scopingObjectKey);
                    merger.ClearIgnoreFlags(scopingObjectKey);
                }

                merger.DetectMergeCandidates(scopingObjectKey);
                return null;
            }
            else
            {
                throw new InvalidOperationException("Cannot determine the operation");
            }
        }
    }
}
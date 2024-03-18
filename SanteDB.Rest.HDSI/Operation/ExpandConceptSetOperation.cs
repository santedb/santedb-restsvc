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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Expand a concept set into a dropdown
    /// </summary>
    public class ExpandConceptSetOperation : IApiChildOperation
    {
        // Concept repository service
        private readonly IConceptRepositoryService m_conceptRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ExpandConceptSetOperation(IConceptRepositoryService conceptRepository)
        {
            this.m_conceptRepository = conceptRepository;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance | ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(ConceptSet) };

        /// <inheritdoc/>
        public string Name => "expand";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {

            IQueryResultSet<Concept> results = null;
            if (scopingKey is Guid uuid)
            {
                results = this.m_conceptRepository.ExpandConceptSet(uuid);
            }
            else if (parameters.TryGet("_mnemonic", out String mnemonic))
            {
                results = this.m_conceptRepository.ExpandConceptSet(mnemonic);
            }
            else
            {
                throw new ArgumentNullException("mnemonic");
            }

            // Is there a filter?
            var filter = parameters.Parameters.ToDictionaryIgnoringDuplicates(o => o.Name, o => o.Value).ToNameValueCollection();
            var linq = QueryExpressionParser.BuildLinqExpression<Concept>(filter);
            var outputResults = results.Where(linq).ApplyResultInstructions(filter, out var offset, out var count).OfType<Concept>();
            return new Bundle(outputResults, offset, count);
        }
    }
}

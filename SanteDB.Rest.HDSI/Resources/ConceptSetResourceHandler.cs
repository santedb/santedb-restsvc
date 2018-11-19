﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * Date: 2017-9-1
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using SanteDB.Core.Interop;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Core;

namespace SanteDB.Rest.HDSI.Resources
{
	/// <summary>
	/// Resource handler for concept sets
	/// </summary>
	public class ConceptSetResourceHandler : IResourceHandler
	{
		/// <summary>
		/// The internal reference to the <see cref="IConceptRepositoryService"/> instance.
		/// </summary>
		private IConceptRepositoryService repositoryService;

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Get the capabilities of this handler
        /// </summary>
        public ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Create | ResourceCapability.CreateOrUpdate | ResourceCapability.Delete | ResourceCapability.Get | ResourceCapability.Search | ResourceCapability.Update;
            }
        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "ConceptSet";

		/// <summary>
		/// Gets the type of serialization
		/// </summary>
		public Type Type => typeof(ConceptSet);

		/// <summary>
		/// Creates the specified data
		/// </summary>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public Object Create(Object data, bool updateIfExists)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			var bundleData = data as Bundle;

			bundleData?.Reconstitute();

			var processData = bundleData?.Entry ?? data;

			if (processData is Bundle)
			{
				throw new InvalidOperationException("Bundle must have entry of type ConceptSet");
			}

			if (processData is ConceptSet)
			{
				return updateIfExists ? this.repositoryService.SaveConceptSet(processData as ConceptSet) : this.repositoryService.InsertConceptSet(processData as ConceptSet);
			}

			throw new ArgumentException("Invalid persistence type");
		}

		/// <summary>
		/// Gets the specified conceptset
		/// </summary>
		public Object Get(object id, object versionId)
		{
			if ((Guid)versionId != Guid.Empty)
			{
				throw new NotSupportedException();
			}

			return this.repositoryService.GetConceptSet((Guid)id);
		}

		/// <summary>
		/// Obsolete the specified concept set
		/// </summary>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public Object Obsolete(object key)
		{
			return this.repositoryService.ObsoleteConceptSet((Guid)key);
		}

		/// <summary>
		/// Perform query
		/// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IEnumerable<Object> Query(NameValueCollection queryParameters)
		{
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
		}

        /// <summary>
        /// Query with specified parameter data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
		{
            var filter = QueryExpressionParser.BuildLinqExpression<ConceptSet>(queryParameters);
            List<String> queryId = null;
            if (this.repositoryService is IPersistableQueryRepositoryService && queryParameters.TryGetValue("_queryId", out queryId))
                return (this.repositoryService as IPersistableQueryRepositoryService).Find(filter, offset, count, out totalCount, Guid.Parse(queryId[0]));
            else
                return this.repositoryService.FindConceptSets(filter, offset, count, out totalCount);
		}

		/// <summary>
		/// Update the specified object
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public Object Update(Object  data)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			var bundleData = data as Bundle;

			bundleData?.Reconstitute();

			var processData = bundleData?.Entry ?? data;

			if (processData is Bundle)
			{
				throw new InvalidOperationException("Bundle must have entry of type Concept");
			}

			if (processData is ConceptSet)
			{
				return this.repositoryService.SaveConceptSet(processData as ConceptSet);
			}

			throw new ArgumentException("Invalid persistence type");
		}
	}
}
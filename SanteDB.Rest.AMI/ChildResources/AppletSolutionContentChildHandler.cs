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
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.Query;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Applet solution content
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AppletSolutionContentChildHandler : IApiChildResourceHandler
    {
        /// <summary>
        /// Get the types this binds to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(AppletSolution) };

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string Name => "applet";

        /// <summary>
        /// Gets the type of object returned
        /// </summary>
        public Type PropertyType => typeof(AppletPackage);

        // Solution Manager
        private IAppletSolutionManagerService m_solutionManager;

        /// <summary>
        /// Creates a new content child handler
        /// </summary>
        /// <param name="solutionManager">The solution manager</param>
        public AppletSolutionContentChildHandler(IAppletSolutionManagerService solutionManager)
        {
            this.m_solutionManager = solutionManager;
        }

        /// <summary>
        /// Gets the capabilities of this
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Adding applets is not yet supported
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get specific applet
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var appletData = this.m_solutionManager.GetPackage(scopingKey.ToString(), key.ToString());

            if (appletData == null)
                throw new FileNotFoundException(key.ToString());
            else
            {
                var appletManifest = AppletPackage.Load(appletData);
                RestOperationContext.Current.OutgoingResponse.SetETag(appletManifest.Meta.Version);
                RestOperationContext.Current.OutgoingResponse.Headers.Add("X-SanteDB-PakID", appletManifest.Meta.Id);
                if (appletManifest.Meta.Hash != null)
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("X-SanteDB-Hash", Convert.ToBase64String(appletManifest.Meta.Hash));
                RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Type", "application/octet-stream");
                RestOperationContext.Current.OutgoingResponse.ContentType = "application/octet-stream";
                RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Disposition", $"attachment; filename=\"{appletManifest.Meta.Id}.pak.gz\"");
                RestOperationContext.Current.OutgoingResponse.AppendHeader("Location", $"/ami/Applet/{appletManifest.Meta.Id}");
                return new MemoryStream(appletData);
            }
        }

        /// <summary>
        /// Query for applet
        /// </summary>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletManifest>(filter);
            var applets = this.m_solutionManager.GetApplets(scopingKey.ToString()).Where(query.Compile()).Select(o => new AppletManifestInfo(o.Info, null));
            return new MemoryQueryResultSet(applets);
        }

        /// <summary>
        /// Remove a sub-solution
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}
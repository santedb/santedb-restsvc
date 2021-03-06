﻿using RestSrvr;
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
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Applet solution content
    /// </summary>
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
        public IEnumerable<object> Query(Type scopingType, object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletManifest>(filter);
            var applets = this.m_solutionManager.GetApplets(scopingKey.ToString()).Where(query.Compile()).Select(o => new AppletManifestInfo(o.Info, null));
            totalCount = applets.Count();
            return applets.Skip(offset).Take(count).OfType<Object>();
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

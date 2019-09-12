/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: justi
 * Date: 2019-1-12
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler utility
    /// </summary>
    public class ResourceHandlerTool : IAuditEventSource, ISecurityAuditEventSource
	{
		// Resource handler utility classes
		private static object m_lockObject = new object();

        // Common trace
        private Tracer m_traceSource = Tracer.GetTracer(typeof(ResourceHandlerTool));

		// Handlers
		private Dictionary<String, IApiResourceHandler> m_handlers = new Dictionary<string, IApiResourceHandler>();

        public event EventHandler<AuditDataEventArgs> DataCreated;
        public event EventHandler<AuditDataEventArgs> DataUpdated;
        public event EventHandler<AuditDataEventArgs> DataObsoleted;
        public event EventHandler<AuditDataDisclosureEventArgs> DataDisclosed;
        public event EventHandler<SecurityAuditDataEventArgs> SecurityAttributesChanged;
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceCreated;
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceDeleted;

        /// <summary>
        /// Get the current handlers
        /// </summary>
        public IEnumerable<IApiResourceHandler> Handlers => this.m_handlers.Values;

        /// <summary>
        /// Creates an single resource handler for a particular service
        /// </summary>
        /// <param name="resourceTypes">The type of resource handlers</param>
        public ResourceHandlerTool(IEnumerable<Type> resourceHandlerTypes, Type scope)
        {
            foreach (var t in resourceHandlerTypes.Where(t=>!t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface))
            {
                try
                {
                    ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                    IApiResourceHandler rh = ci.Invoke(null) as IApiResourceHandler;
                    if (rh.Scope == scope)
                    {
                        this.m_handlers.Add($"{rh.Scope.Name}/{rh.ResourceName}", rh);
                        this.m_traceSource.TraceInfo("Adding {0} to {1}", rh.ResourceName, rh.Scope);

                        // Pass through audit events
                        if (rh is IAuditEventSource)
                        {
                            var raes = rh as IAuditEventSource;
                            raes.DataCreated += (o, e) => this.DataCreated?.Invoke(o, e);
                            raes.DataDisclosed += (o, e) => this.DataDisclosed?.Invoke(o, e);
                            raes.DataObsoleted += (o, e) => this.DataObsoleted?.Invoke(o, e);
                            raes.DataUpdated += (o, e) => this.DataUpdated?.Invoke(o, e);
                        }
                        if(rh is ISecurityAuditEventSource)
                        {
                            var isaes = rh as ISecurityAuditEventSource;
                            isaes.SecurityAttributesChanged += (o, e) => this.SecurityAttributesChanged?.Invoke(o, e);
                            isaes.SecurityResourceCreated += (o, e) => this.SecurityResourceCreated?.Invoke(o, e);
                            isaes.SecurityResourceDeleted += (o, e) => this.SecurityResourceDeleted?.Invoke(o, e);
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error binding: {0} due to {1}", t.FullName, e);
                }
            }
        }

		/// <summary>
		/// Get resource handler
		/// </summary>
		public IApiResourceHandler GetResourceHandler<TScope>(String resourceName)
		{
			IApiResourceHandler retVal = null;
			this.m_handlers.TryGetValue($"{typeof(TScope).Name}/{resourceName}", out retVal);
			return retVal;
		}
	}
}
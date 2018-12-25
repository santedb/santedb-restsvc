/*
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
 * User: justin
 * Date: 2018-11-19
 */
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler utility
    /// </summary>
    public class ResourceHandlerTool
	{
		// Resource handler utility classes
		private static object m_lockObject = new object();

        // Common trace
        private Tracer m_traceSource = Tracer.GetTracer(typeof(ResourceHandlerTool));

		// Handlers
		private Dictionary<String, IApiResourceHandler> m_handlers = new Dictionary<string, IApiResourceHandler>();

		/// <summary>
		/// Get the current handlers
		/// </summary>
		public IEnumerable<IApiResourceHandler> Handlers => this.m_handlers.Values;

        /// <summary>
        /// Creates an single resource handler for a particular service
        /// </summary>
        /// <param name="resourceTypes">The type of resource handlers</param>
        public ResourceHandlerTool(IEnumerable<Type> resourceHandlerTypes)
        {
            foreach (var t in resourceHandlerTypes.Where(t=>!t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface))
            {
                try
                {
                    ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                    IApiResourceHandler rh = ci.Invoke(null) as IApiResourceHandler;
                    this.m_handlers.Add($"{rh.Scope.Name}/{rh.ResourceName}", rh);
                    this.m_traceSource.TraceInfo("Adding {0} to {1}", rh.ResourceName, rh.Scope);
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
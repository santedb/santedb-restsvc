/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler utility
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class ResourceHandlerTool
    {

        private static readonly IServiceManager s_serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
        private static readonly IEnumerable<IApiChildResourceHandler> s_childResourceHandlers;
        private static readonly IEnumerable<IApiChildOperation> s_operationHandlers;

        static ResourceHandlerTool()
        {
            s_childResourceHandlers = s_serviceManager.CreateInjectedOfAll<IApiChildResourceHandler>().ToList();
            s_operationHandlers = s_serviceManager.CreateInjectedOfAll<IApiChildOperation>().ToList();
        }

        // Common trace
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(ResourceHandlerTool));

        // Handlers
        private ConcurrentDictionary<String, IApiResourceHandler> m_handlers = new ConcurrentDictionary<string, IApiResourceHandler>();

        /// <summary>
        /// Get the current handlers
        /// </summary>
        public IEnumerable<IApiResourceHandler> Handlers => this.m_handlers.Values;

        /// <summary>
        /// Creates an single resource handler for a particular service
        /// </summary>
        /// <param name="resourceHandlerTypes">The type of resource handlers</param>
        /// <param name="scope">The scope in which the handler types operate</param>
        public ResourceHandlerTool(IEnumerable<Type> resourceHandlerTypes, Type scope)
        {
            
            foreach (var t in resourceHandlerTypes.Where(t => !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface))
            {
                try
                {
                    IApiResourceHandler rh = s_serviceManager.CreateInjected(t) as IApiResourceHandler;
                    if (rh == null)
                    {
                        continue; // TODO: Emit a warning
                    }

                    if (rh.Scope == scope)
                    {
                        this.m_handlers.TryAdd($"{rh.Scope.Name}/{rh.ResourceName}", rh);
                        this.m_traceSource.TraceVerbose("Adding {0} to {1}", rh.ResourceName, rh.Scope);

                        // Associated prop handler
                        if (rh is IChainedApiResourceHandler assoc)
                        {
                            s_childResourceHandlers.Where(p => p.ParentTypes.Contains(rh.Type) || p.ParentTypes == Type.EmptyTypes).ToList().ForEach(p => assoc.AddChildResource(p));
                        }
                        if (rh is IOperationalApiResourceHandler oper)
                        {
                            s_operationHandlers.Where(p => p.ParentTypes.Contains(rh.Type) || p.ParentTypes == Type.EmptyTypes).ToList().ForEach(p => oper.AddOperation(p));
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceWarning("Error binding: {0} due to {1}", t.FullName, e.Message);
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

        /// <summary>
        /// Get resource handler
        /// </summary>
        public IEnumerable<IApiResourceHandler> GetResourceHandler(Type resourceType)
        {
            return this.m_handlers.Values.Where(o => o.Type == resourceType);
        }
    }
}
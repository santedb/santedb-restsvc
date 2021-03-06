﻿/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using System;
using System.Collections.Concurrent;
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

        // Common trace
        private Tracer m_traceSource = Tracer.GetTracer(typeof(ResourceHandlerTool));

        // Handlers
        private ConcurrentDictionary<String, IApiResourceHandler> m_handlers = new ConcurrentDictionary<string, IApiResourceHandler>();

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
            var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var tPropertyProviders = serviceManager.CreateInjectedOfAll<IApiChildResourceHandler>();
            var tOperationProviders = serviceManager.CreateInjectedOfAll<IApiChildOperation>();

            foreach (var t in resourceHandlerTypes.Where(t => !t.ContainsGenericParameters && !t.IsAbstract && !t.IsInterface))
            {
                try
                {

                    IApiResourceHandler rh = serviceManager.CreateInjected(t) as IApiResourceHandler;
                    if (rh == null)
                        continue; // TODO: Emit a warning


                    if (rh.Scope == scope)
                    {
                        this.m_handlers.TryAdd($"{rh.Scope.Name}/{rh.ResourceName}", rh);
                        this.m_traceSource.TraceInfo("Adding {0} to {1}", rh.ResourceName, rh.Scope);

                        // Associated prop handler
                        if (rh is IChainedApiResourceHandler assoc)
                        {
                            tPropertyProviders.Where(p => p.ParentTypes.Contains(rh.Type)).ToList().ForEach(p => assoc.AddChildResource(p));
                        }
                        if(rh is IOperationalApiResourceHandler oper)
                        {
                            tOperationProviders.Where(p => p.ParentTypes.Contains(rh.Type)).ToList().ForEach(p => oper.AddOperation(p));
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

        /// <summary>
        /// Get resource handler
        /// </summary>
        public IEnumerable<IApiResourceHandler> GetResourceHandler(Type resourceType)
        {
            return this.m_handlers.Values.Where(o=>o.Type == resourceType);
        }

    }
}
/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2024-12-12
 */
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Extension handler class fetch function
    /// </summary>
    /// <remarks>This is an operation rather than a sub-resource as it returns a parameter collection</remarks>
    public class ExtensionHandlerClassOperation : IApiChildOperation
    {

        private readonly IExtensionHandler[] m_extensionHandlers;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExtensionHandlerClassOperation()
        {
            this.m_extensionHandlers = AppDomain.CurrentDomain.GetAllTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(IExtensionHandler).IsAssignableFrom(t))
                .Select(t =>
                {
                    try
                    {
                        return Activator.CreateInstance(t) as IExtensionHandler;
                    }
                    catch
                    {
                        return null;
                    }
                }).OfType<IExtensionHandler>().ToArray();
        }
        /// <inheritdoc/>
        public string Name => "handlers";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(ExtensionType) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            return new ParameterCollection(this.m_extensionHandlers.Select(h => new Parameter(h.Name, h.GetType().AssemblyQualifiedNameWithoutVersion())).ToArray());
        }
    }
}

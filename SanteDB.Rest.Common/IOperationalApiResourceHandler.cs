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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represnets a resource handler which can link sub-objects (or certain sub-objects) with a parent object
    /// </summary>
    public interface IOperationalApiResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Gets the associated resources
        /// </summary>
        IEnumerable<IApiChildOperation> Operations { get; }

        /// <summary>
        /// Add a property handler
        /// </summary>
        /// <param name="operation">The operation to be added to the resource handler</param>
        void AddOperation(IApiChildOperation operation);

        /// <summary>
        /// Fetchs the scoped entity
        /// </summary>
        /// <param name="operationName">The operation to be invoked</param>
        /// <param name="parameters">The parameter to pass to the operation</param>
        /// <param name="scopingEntityKey">The scoped entity key or null if operating on the type rather than an instance</param>
        Object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters);

        /// <summary>
        /// Try to get chianed resource
        /// </summary>
        /// <param name="bindingType">The type of binding</param>
        /// <param name="operationName">The property name to obtain</param>
        /// <param name="operationHandler">The operation handler</param>
        bool TryGetOperation(string operationName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler);
    }
}
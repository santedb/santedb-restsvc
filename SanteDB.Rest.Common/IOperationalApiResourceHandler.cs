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
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        void AddOperation(IApiChildOperation property);

        /// <summary>
        /// Fetchs the scoped entity
        /// </summary>
        Object InvokeOperation(object scopingEntityKey, string operationName, ApiOperationParameterCollection parameters);
    }
}

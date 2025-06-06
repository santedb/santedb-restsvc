﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using System;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler that can lock or unlock objects
    /// </summary>
    public interface ILockableResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Locks a resource.
        /// </summary>
        /// <param name="key">The key of the resource to Locks.</param>
        /// <returns>Returns the locked object</returns>
        Object Lock(Object key);

        /// <summary>
        /// Obsoletes a unlock.
        /// </summary>
        /// <param name="key">The key of the resource to unlock.</param>
        /// <returns>Returns the unlock object.</returns>
        Object Unlock(Object key);
    }
}

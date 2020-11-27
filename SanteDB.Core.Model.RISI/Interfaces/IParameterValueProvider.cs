﻿/*
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using System;
using System.Collections.Generic;

namespace SanteDB.Core.Model.RISI.Interfaces
{
    /// <summary>
    /// Represents a parameter provider.
    /// </summary>
    public interface IParameterValuesProvider
    {
        /// <summary>
        /// Gets or sets the query identifier.
        /// </summary>
        /// <value>The query identifier.</value>
        Guid QueryId { get; }

        /// <summary>
        /// Gets a list of values.
        /// </summary>
        /// <typeparam name="T">The type of parameter for which to retrieve values.</typeparam>
        /// <returns>Returns a list of values.</returns>
        IEnumerable<T> GetValues<T>() where T : IdentifiedData;
    }
}
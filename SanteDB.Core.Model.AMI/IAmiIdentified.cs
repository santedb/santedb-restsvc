/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using System;

namespace SanteDB.Core.Model.AMI
{
    /// <summary>
    /// Represents an interface to extract the URL identifier path
    /// </summary>
    public interface IAmiIdentified
    {

        /// <summary>
        /// Get the desired url resource key 
        /// </summary>
        String Key { get; set; }

        /// <summary>
        /// Gets the tag for the resource
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// Get the modified on
        /// </summary>
        DateTimeOffset ModifiedOn { get; }
    }
}

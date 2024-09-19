/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using System;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Constants for https://help.santesuite.org/developers/service-apis/health-data-service-interface-hdsi/hdsi-query-syntax#control-parameters
    /// </summary>
    public static class QueryControlParameterNames
    {

        /// <summary>
        /// The view model to use to serialize the response
        /// </summary>
        public const String HttpViewModelParameterName = "_viewModel";

        /// <summary>
        /// Indicates the server should bundle dependent objects
        /// </summary>
        public const String HttpBundleRelatedParameterName = "_bundle";

        /// <summary>
        /// The name of the HTTP upstream instruction
        /// </summary>
        public const String HttpSinceParameterName = "_since";

        /// <summary>
        /// The name of the HTTP upstream instruction
        /// </summary>
        public const String HttpUpstreamParameterName = "_upstream";

        /// <summary>
        /// The name of the HTTP parameter that limits the number of results returned
        /// </summary>
        public const String HttpCountParameterName = "_count";

        /// <summary>
        /// The name of the HTTP parameter that sets the offset of the first result
        /// </summary>
        public const String HttpOffsetParameterName = "_offset";

        /// <summary>
        /// The name of the HTTP parameter which contains the ordering instructions
        /// </summary>
        public const String HttpOrderByParameterName = "_orderBy";

        /// <summary>
        /// The name of the HTTP parameter which contains the query identifier
        /// </summary>
        public const String HttpQueryStateParameterName = "_queryId";

        /// <summary>
        /// NAme of the HTTP parameter to count all results
        /// </summary>
        public const String HttpIncludeTotalParameterName = "_includeTotal";
        /// <summary>
        /// Name of the include path
        /// </summary>
        public const String HttpIncludePathParameterName = "_include";
        /// <summary>
        /// Name of the exclude path
        /// </summary>
        public const String HttpExcludePathParameterName = "_exclude";
    }
}

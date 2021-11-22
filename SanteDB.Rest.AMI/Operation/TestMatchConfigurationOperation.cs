/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */

using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Matching;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Test the match configuration REST operation
    /// </summary>
    public class TestMatchConfigurationOperation : IApiChildOperation
    {
        // Config service
        private IRecordMatchingConfigurationService m_configService;

        /// <summary>
        /// Create a new match configuration operation
        /// </summary>
        public TestMatchConfigurationOperation(IRecordMatchingConfigurationService configService = null)
        {
            this.m_configService = configService;
        }

        /// <summary>
        /// Gets the type to bind to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(IRecordMatchingConfiguration) };

        /// <summary>
        /// Gets the property name
        /// </summary>
        public string Name => "test";

        /// <summary>
        /// Test the match configuration is an instance method
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        ///
        /// </summary>
        /// <param name="scopingType"></param>
        /// <param name="scopingKey"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            throw new NotSupportedException();
        }
    }
}
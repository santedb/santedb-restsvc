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
 * Date: 2024-6-21
 */
using RestSrvr;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Cdss.Xml.Ami
{
    /// <summary>
    /// Represents an operation which will take a JSON object and expand it using the specified view model
    /// </summary>
    public class ToViewModelOperation : IApiChildOperation
    {
        /// <inheritdoc/>
        public string Name => "expand";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] {
            typeof(Entity),
            typeof(Person),
            typeof(Material),
            typeof(ManufacturedMaterial),
            typeof(Place),
            typeof(Organization),
            typeof(Patient),
            typeof(Act),
            typeof(SubstanceAdministration),
            typeof(CodedObservation),
            typeof(QuantityObservation),
            typeof(TextObservation),
            typeof(Procedure),
            typeof(PatientEncounter),
            typeof(Narrative)
        };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName]))
            {
                RestOperationContext.Current.IncomingRequest.Headers.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, "full");
            }
            RestOperationContext.Current.OutgoingResponse.ContentType = SanteDBExtendedMimeTypes.JsonViewModel; // "application/json+sdb-viewmodel";

            if (parameters.TryGet("object", out object objectPayload))
            {
                return objectPayload;
            }
            else
            {
                throw new ArgumentNullException("object");
            }



        }
    }
}

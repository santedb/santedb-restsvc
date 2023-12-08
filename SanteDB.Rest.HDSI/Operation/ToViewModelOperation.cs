using RestSrvr;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Cdss;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            var objectPayload = parameters.Parameters.Find(o => o.Name == "object")?.Value;
            if(objectPayload == null)
            {
                throw new ArgumentNullException("object");
            }
            else if(objectPayload is String str)
            {
                using (var jvu = new JsonViewModelSerializer())
                {
                    objectPayload = jvu.DeSerialize<IdentifiedData>(str);
                }
            }

            if (String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName]))
            {
                RestOperationContext.Current.IncomingRequest.Headers.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, "full");
            }
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/json+sdb-viewmodel";
            return objectPayload;
        }
    }
}

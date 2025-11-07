using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Alter policies on an object
    /// </summary>
    public class AlterPolicyOperation : IApiChildOperation
    {

        /// <summary>
        /// Cascade policies
        /// </summary>
        public const string PARAMETER_NAME_CASCADE = "cascadePolicies";
        /// <summary>
        /// Remove policies
        /// </summary>
        public const string PARAMETER_NAME_REMOVE = "remove";
        /// <summary>
        /// Add policies
        /// </summary>
        public const string PARAMETER_NAME_ADD = "add";

        /// <summary>
        /// Tracer
        /// </summary>
        private readonly Tracer m_tracer;
        /// <summary>
        /// PIP service
        /// </summary>
        private readonly IPolicyInformationService m_pipService;
        /// <summary>
        /// Auditing service
        /// </summary>
        private readonly IAuditService m_auditService;
        private readonly IDataPersistenceService<Act> m_actPersistence;
        private readonly IDataPersistenceService<Entity> m_entityPersistence;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AlterPolicyOperation(IPolicyInformationService pipService, IAuditService auditService, IDataPersistenceService<Act> actPersistence, IDataPersistenceService<Entity> entityPersistence)
        {
            this.m_pipService = pipService;
            this.m_auditService = auditService;
            this.m_actPersistence = actPersistence; ;
            this.m_entityPersistence = entityPersistence;
        }

        /// <inheritdoc/>
        public string Name => "alter-policy";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Entity),
            typeof(Act)
        };

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AssignPolicy)]
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingType == null)
            {
                throw new ArgumentNullException();
            }
            if (!(scopingKey is Guid scopeUuid) || !Guid.TryParse(scopingKey.ToString(), out scopeUuid))
            {
                throw new ArgumentNullException(nameof(scopingKey));
            }

            var audit = this.m_auditService.Audit()
                .WithAction(Core.Model.Audit.ActionType.Update)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.SecurityAlert)
                .WithEventType(ExtendedAuditCodes.EventTypePrivacy)
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
                .WithLocalDestination()
                .WithPrincipal(AuthenticationContext.Current.Principal)
                .WithTimestamp(DateTimeOffset.Now);

            try
            {
                // Attempt to get the object and apply to all of its 
                _ = parameters.TryGet(PARAMETER_NAME_CASCADE, out bool cascadePolicies);

                if (!parameters.TryGet(PARAMETER_NAME_REMOVE, out String[] removePolicies) ||
                    !parameters.TryGet(PARAMETER_NAME_ADD, out String[] addPolicies))
                {
                    throw new ArgumentNullException(PARAMETER_NAME_ADD);
                }

                using (DataPersistenceControlContext.Create(loadMode: LoadMode.QuickLoad))
                {
                    object targetObj = scopingType == typeof(Act) ? (object)this.m_actPersistence.Get(scopeUuid, null, AuthenticationContext.Current.Principal) :
                        this.m_entityPersistence.Get(scopeUuid, null, AuthenticationContext.Current.Principal);

                    if (targetObj == null)
                    {
                        throw new KeyNotFoundException($"{scopingType.GetSerializationName()}/{scopeUuid}");
                    }


                    audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, targetObj);

                    // Apply to related objects?
                    switch (targetObj)
                    {
                        case Entity ent:
                            this.ApplyPolicyChange(ent, addPolicies, removePolicies);
                            if (cascadePolicies)
                            {
                                var acts = this.m_actPersistence.Query(o => o.Participations.Where(p => p.ParticipationRoleKey == ActParticipationKeys.RecordTarget).Any(p => p.PlayerEntityKey == ent.Key), AuthenticationContext.SystemPrincipal);
                                foreach (var a in acts)
                                {
                                    this.ApplyPolicyChange(a, addPolicies, removePolicies);
                                    audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, a);
                                }
                            }
                            break;
                        case Act act:
                            this.ApplyPolicyChange(act, addPolicies, removePolicies);
                            if (cascadePolicies)
                            {
                                var related = this.m_actPersistence.Query(o => o.Relationships.Where(p => p.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent || p.ClassificationKey == RelationshipClassKeys.ContainedObjectLink).Any(p => p.SourceEntityKey == act.Key), AuthenticationContext.SystemPrincipal);
                                foreach (var a in related)
                                {
                                    this.ApplyPolicyChange(a, addPolicies, removePolicies);
                                    audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, act);
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
                return null;
            }
            catch
            {
                audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            finally
            {
                audit.Send();
            }

        }

        /// <summary>
        /// Apply the policy changes
        /// </summary>
        private void ApplyPolicyChange(object targetObj, string[] addPolicies, string[] removePolicies)
        {
            if (removePolicies.Length > 0)
            {
                this.m_pipService.RemovePolicies(targetObj, AuthenticationContext.Current.Principal, removePolicies);
            }

            if (addPolicies.Length > 0)
            {
                this.m_pipService.AddPolicies(targetObj, Core.Model.Security.PolicyGrantType.Grant, AuthenticationContext.Current.Principal, addPolicies);
            }

        }
    }
}

﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using RestSrvr.Attributes;
using SanteDB.Client.Tickles;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Configuration;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Templates.Definition;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service contract
    /// </summary>
    [ServiceContract(Name = "APP")]
    [ServiceProduces("application/json")]
    [ServiceProduces(SanteDBExtendedMimeTypes.JsonViewModel)]
    [ServiceConsumes(SanteDBExtendedMimeTypes.JsonViewModel)]
    [ServiceConsumes("application/json")]
    [ServiceKnownResource(typeof(Menu))]
    [ServiceKnownResource(typeof(Client.Tickles.Tickle))]
    [RestServiceFault(400, "The provided resource was in an incorrect format")]
    [RestServiceFault(401, "The principal is unauthorized and needs to either elevate or authenticate themselves")]
    [RestServiceFault(403, "The principal is not permitted (cannot elevate) to perform the operation")]
    [RestServiceFault(404, "The requested object does not exist")]
    [RestServiceFault(410, "The specified object did exist however is no-longer present")]
    [RestServiceFault(415, "The client is submitting an invalid object")]
    [RestServiceFault(422, "There was a business rule violation executing the operation")]
    [RestServiceFault(429, "The server rejected the request due to a throttling constraint")]
    [RestServiceFault(500, "The server encountered an error processing the result")]
    [RestServiceFault(503, "The service is not available (starting up or shutting down)")]
    public interface IAppServiceContract
    {
        #region Configuration
        /// <summary>
        /// Get the configuration
        /// </summary>
        [Get("/Configuration")]
        ConfigurationViewModel GetConfiguration();

        /// <summary>
        /// Update configuration
        /// </summary>
        [Post("/Configuration")]
        ConfigurationViewModel UpdateConfiguration(ConfigurationViewModel configuration);

        /// <summary>
        /// Update configuration
        /// </summary>
        [Get("/Configuration/{userName}/settings")]
        List<AppSettingKeyValuePair> GetAppSettings(String userName);

        /// <summary>
        /// Update configuration
        /// </summary>
        [Post("/Configuration/{userName}/settings")]
        void SetAppSettings(String userName, List<AppSettingKeyValuePair> settings);

        /// <summary>
        /// Disable the specified service
        /// </summary>
        [Get("/Configuration/Service")]
        List<DiagnosticServiceInfo> GetServices();

        /// <summary>
        /// Disable the specified service
        /// </summary>
        [Delete("/Configuration/Service/{serviceType}")]
        void DisableService(String serviceType);

        /// <summary>
        /// Enable the specified service
        /// </summary>
        [Post("/Configuration/Service/{serviceType}")]
        void EnableService(String serviceType);

        /// <summary>
        /// Get the data storage providers
        /// </summary>
        [Get("/DataProviders")]
        List<StorageProviderViewModel> GetDataStorageProviders();

        /// <summary>
        /// Get the data storage providers
        /// </summary>
        [Get("/IntegrationPatterns")]
        List<String> GetIntegrationPatterns();

        #endregion

        #region Realm
        /// <summary>
        /// Join the realm
        /// </summary>
        [Post("/Realm/$join")]
        ParameterCollection JoinRealm(ParameterCollection parameters);

        /// <summary>
        /// Join the realm
        /// </summary>
        [Post("/Realm/$unjoin")]
        ParameterCollection UnJoinRealm(ParameterCollection parameters);

        /// <summary>
        /// Instruct the service to do an update
        /// </summary>
        [Post("/$update")]
        ParameterCollection PerformUpdate(ParameterCollection parameters);
        #endregion

        #region Templates
        /// <summary>
        /// Get all templates
        /// </summary>
        [Get("/Template")]
        List<TemplateDefinitionViewModel> GetTemplates();

        /// <summary>
        /// Get template 
        /// </summary>
        [Get("/Template/{templateId}")]
        TemplateDefinitionViewModel GetTemplate(string templateId);

        /// <summary>
        /// Gets the specified template identifier
        /// </summary>
        [Get("/Template/{templateId}/skel")]
        IdentifiedData GetTemplateModel(String templateId);

        /// <summary>
        /// Get the view for the specified template
        /// </summary>
        [Get("/Template/{templateId}/ui/view.html")]
        [Obsolete("Kept for backwards compatibility")]
        void GetTemplateView(String templateId);

        /// <summary>
        /// Get the form for the specified template
        /// </summary>
        [Get("/Template/{templateId}/ui/form.html")]
        [Obsolete("Kept for backwards compatibility")]
        void GetTemplateForm(String templateId);

        /// <summary>
        /// Get template view information
        /// </summary>
        /// <param name="templateId">The template identifier</param>
        /// <param name="viewType">The view to obtain</param>
        /// <returns>A stream representing the contents of the form view</returns>
        [Get("/Template/{templateId}/view/{viewType}")]
        Stream GetTemplateView(String templateId, String viewType);

        /// <summary>
        /// Gets the template definition
        /// </summary>
        [Get("/Template/{templateId}/definition.json")]
        Stream GetTemplateDefinition(String templateId);

        #endregion

        #region Tickle
        /// <summary>
        /// Gets the tickles/reminders which are alerts in the application
        /// </summary>
        [Get("/Tickle")]
        List<Tickle> GetTickles();

        /// <summary>
        /// Creates a tickle on the service
        /// </summary>
        [Post("/Tickle")]
        void CreateTickle(Tickle data);

        /// <summary>
        /// Delete the specified tickle
        /// </summary>
        [Delete("/Tickle/{id}")]
        void DismissTickle(Guid id);
        #endregion

        #region User Interface
        /// <summary>
        /// Gets the routes
        /// </summary>
        [Get("/routes.js")]
        Stream GetRoutes();

        /// <summary>
        /// Get locale assets
        /// </summary>
        [Get("/Locale")]
        Dictionary<String, String[]> GetLocaleAssets();

        /// <summary>
        /// Gets menus
        /// </summary>
        [Get("/Menu")]
        List<Menu> GetMenus();

        /// <summary>
        /// Gets a new UUID 
        /// </summary>
        /// <remarks>TODO: Generate sequential UUIDS</remarks>
        [Get("/Uuid")]
        Guid GetUuid();

        /// <summary>
        /// Gets the widgets 
        /// </summary>
        [Get("/Widgets")]
        List<AppletWidget> GetWidgets();

        /// <summary>
        /// Get a widget
        /// </summary>
        [Get("/Widgets/{widgetId}")]
        Stream GetWidget(String widgetId);

        /// <summary>
        /// Get DCG online state
        /// </summary>
        [Get("/State")]
        Dictionary<String, object> GetState();

        /// <summary>
        /// Get current user information
        /// </summary>
        [Get("/SessionInfo")]
        Dictionary<String, Object> GetCurrentSessionInfo();

        #endregion


    }
}

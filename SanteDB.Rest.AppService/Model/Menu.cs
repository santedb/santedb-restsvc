/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Rest.AppService.Model
{
    /// <summary>
    /// A menu item.
    /// </summary>
    [XmlType(nameof(Menu), Namespace = "http://santedb.org/appService")]
    [XmlRoot(nameof(Menu), Namespace = "http://santedb.org/appService")]
    [JsonObject(nameof(Menu))]
    public class Menu
    {
        /// <summary>
        /// Gets or sets the child menu items of this menu.
        /// </summary>
        /// <remarks>Avoid a circular reference. Do not add the current <see cref="Menu"/> instance to this list.</remarks>
        [JsonProperty("menu")]
        public List<Menu> MenuItems { get; set; }
        /// <summary>
        /// Gets or sets the icon for this menu. This is typically one or more CSS classes that compose an icon in the user interface.
        /// </summary>
        /// <example>fas fa-fw fa-user</example>
        [JsonProperty("icon")]
        public string Icon { get; set; }
        /// <summary>
        /// Gets or sets the localized name of the menu item. This is the text that is displayed for the item in the user interface.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the action for the menu item.
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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

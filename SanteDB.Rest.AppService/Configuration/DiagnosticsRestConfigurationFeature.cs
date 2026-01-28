/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using ClosedXML;
using Newtonsoft.Json.Linq;
using SanteDB.Client;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Diagnostics configuration
    /// </summary>
    public class DiagnosticsRestConfigurationFeature : IClientConfigurationFeature
    {
        private readonly DiagnosticsConfigurationSection m_configurationSection;

        /// <summary>
        /// The name of the setting for log detail in the property grid
        /// </summary>
        public const string LOG_DETAIL_SETTING = "mode";
        /// <summary>
        /// The name of hte writer setting for log detail in the property grid
        /// </summary>
        public const string LOG_WRITER_SETTING = "writer";

        /// <summary>
        /// DI constructor
        /// </summary>
        public DiagnosticsRestConfigurationFeature(IConfigurationManager configManager)
        {
            this.m_configurationSection = configManager.GetSection<DiagnosticsConfigurationSection>();
        }

        /// <summary>
        /// Gets the order in which this appears
        /// </summary>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "log";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <inheritdoc/>
        private ConfigurationDictionary<String, Object> GetConfiguration() =>
            new ConfigurationDictionary<string, object>()
            {
                { LOG_DETAIL_SETTING, this.m_configurationSection?.Sources?.Any() == true ? this.m_configurationSection.Sources.Min(o=>o.Filter) : System.Diagnostics.Tracing.EventLevel.Warning },
                { LOG_WRITER_SETTING, Tracer
                    .GetAvailableWriters()
                    .Select(o=> new { name = o.Name, aqn = o.AssemblyQualifiedName, mode = this.m_configurationSection?.TraceWriter.FirstOrDefault(w=>w.TraceWriter == o)?.Filter  })
                    .ToArray()
                }
            };

        /// <inheritdoc/>
        public string ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<DiagnosticsConfigurationSection>();
            if (section == null)
            {
                section = new DiagnosticsConfigurationSection()
                {
                    Mode = System.Diagnostics.Tracing.EventLevel.Warning,
                    TraceWriter = new List<TraceWriterConfiguration>()
                    {
                        new TraceWriterConfiguration()
                        {
                            Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                            TraceWriter = typeof(RolloverTextWriterTraceWriter)
                        }
                    }
                };
            }

            // HACK: Previous versions would not set the sources so let's add the default
            if(section.Sources?.Any() != true)
            {
                section.Sources = section.Sources ?? new List<TraceSourceConfiguration>();
                section.Sources.Add(
                    new TraceSourceConfiguration()
                    {
                        Filter = EventLevel.LogAlways,
                        SourceName = "SanteDB"
                    }
                );
            }

            if (Enum.TryParse<EventLevel>(featureConfiguration[LOG_DETAIL_SETTING]?.ToString(), out var mode))
            {
                section.Mode = mode;
                section.Sources.ForEach(o => o.Filter = mode);
            }

            if (featureConfiguration[LOG_WRITER_SETTING] is IEnumerable enu)
            {
                foreach (JObject itm in enu)
                {
                    var logWriter = itm["aqn"];

                    var logWriterType = Type.GetType(logWriter.ToString());
                    var existingConfig = section.TraceWriter.Find(o => o.TraceWriter == logWriterType);

                    if (!Enum.TryParse<EventLevel>(itm[LOG_DETAIL_SETTING]?.ToString(), out var logMode))
                    {
                        if (existingConfig != null)
                        {
                            section.TraceWriter.Remove(existingConfig);
                        }
                        continue;
                    }
                    else if (existingConfig == null)
                    {
                        existingConfig = new TraceWriterConfiguration()
                        {
                            Filter = logMode,
                            InitializationData = Path.Combine(AppDomain.CurrentDomain.GetData(ClientApplicationContextBase.AppDataDirectorySetting)?.ToString(), "santedb.log"),
                            WriterName = logWriterType.Name,
                            TraceWriter = logWriterType
                        };
                    }
                    else
                    {
                        existingConfig.Filter = logMode;

                    }

                }
            }

            return true;
        }
    }
}

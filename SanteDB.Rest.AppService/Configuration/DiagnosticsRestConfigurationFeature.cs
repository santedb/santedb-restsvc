using SanteDB.Client;
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
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Diagnostics configuration
    /// </summary>
    public class DiagnosticsRestConfigurationFeature : IRestConfigurationFeature
    {
        private readonly DiagnosticsConfigurationSection m_configurationSection;

        public const string LOG_DETAIL_SETTING = "mode";
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
        public RestConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <inheritdoc/>
        private RestConfigurationDictionary<String, Object> GetConfiguration() =>
            new RestConfigurationDictionary<string, object>()
            {
                { LOG_DETAIL_SETTING, this.m_configurationSection?.Mode ?? System.Diagnostics.Tracing.EventLevel.Warning },
                { LOG_WRITER_SETTING, Tracer
                    .GetAvailableWriters()
                    .Select(o=> new { name = o.Name, aqn = o.AssemblyQualifiedName, mode = this.m_configurationSection?.TraceWriter.FirstOrDefault(w=>w.TraceWriter == o)?.Filter  })
                    .ToArray()
                }
            };

        /// <inheritdoc/>
        public string ReadPolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<DiagnosticsConfigurationSection>();
            if(section == null)
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

            if (Enum.TryParse<EventLevel>(featureConfiguration[LOG_DETAIL_SETTING]?.ToString(), out var mode))
            {
                section.Mode = mode;
            }

            if (featureConfiguration[LOG_WRITER_SETTING] is IEnumerable enu)
            {
                foreach(IDictionary itm in enu)
                {
                    if(!Enum.TryParse<EventLevel>(itm[LOG_DETAIL_SETTING]?.ToString(), out var logMode))
                    {
                        continue;
                    }

                    var logWriter = itm["aqn"];

                    var logWriterType = Type.GetType(logWriter.ToString());
                    var existingConfig = section.TraceWriter.Find(o => o.TraceWriter == logWriterType);
                    if(existingConfig == null)
                    {
                        existingConfig = new TraceWriterConfiguration()
                        {
                            Filter = logMode,
                            InitializationData = Path.Combine(AppDomain.CurrentDomain.GetData(ClientApplicationContextBase.AppDataDirectorySetting)?.ToString(), "santedb.log"),
                            WriterName = logWriterType.Name,
                            TraceWriter = logWriterType
                        };
                    }

                }
            }

            return true;
        }
    }
}

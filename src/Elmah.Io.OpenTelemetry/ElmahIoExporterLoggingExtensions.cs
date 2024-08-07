using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elmah.Io.OpenTelemetry
{
    /// <summary>
    /// Extension methods to help install Elmah.Io.OpenTelemetry.
    /// </summary>
    public static class ElmahIoExporterLoggingExtensions
    {
        /// <summary>
        /// Add the elmah.io exporter to OpenTelemetry. Use this method when you are configuring the OpenTelemetryLoggerOptions directly.
        /// </summary>
        public static OpenTelemetryLoggerOptions AddElmahIoExporter(
            this OpenTelemetryLoggerOptions loggerOptions,
            Action<ElmahIoExporterOptions> configure)
        {
            var options = new ElmahIoExporterOptions();
            configure?.Invoke(options);
            return loggerOptions.AddProcessor(new SimpleLogRecordExportProcessor(new ElmahIoExporter(options)));
        }

        /// <summary>
        /// Add the elmah.io exporter to OpenTelemetry. Use this method when you are configuring the LoggerProviderBuilder.
        /// </summary>
        public static LoggerProviderBuilder AddElmahIoExporter(
            this LoggerProviderBuilder loggerProviderBuilder,
            string? name,
            Action<ElmahIoExporterOptions>? configure)
        {
            name ??= Options.DefaultName;

            if (configure != null)
            {
                loggerProviderBuilder.ConfigureServices(services => services.Configure(name, configure));
            }

            return loggerProviderBuilder.AddProcessor(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<ElmahIoExporterOptions>>().Get(name);

                return new SimpleLogRecordExportProcessor(new ElmahIoExporter(options));
            });
        }
    }
}

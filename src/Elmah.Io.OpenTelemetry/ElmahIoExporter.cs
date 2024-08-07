using Elmah.Io.Client;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using static Elmah.Io.OpenTelemetry.UserAgentHelper;

namespace Elmah.Io.OpenTelemetry
{
    /// <summary>
    /// An OpenTelemetry exporter that stores LogRecord objects in elmah.io.
    /// </summary>
    public class ElmahIoExporter(ElmahIoExporterOptions options) : BaseExporter<LogRecord>
    {
        private const string OriginalFormatPropertyKey = "{OriginalFormat}";
        private readonly ElmahIoExporterOptions options = options;
        private IElmahioAPI? elmahIoClient;
        private readonly object syncObject = new();
        private bool disposed;
        private bool isDisposeMessageSent;

        internal ElmahIoExporter(IElmahioAPI elmahIoClient, ElmahIoExporterOptions options) : this(options)
        {
            this.elmahIoClient = elmahIoClient;
        }

        /// <summary>
        /// Store a batch of LogRecord objects in elmah.io. This method is called by OpenTelemetry and should not be called manually.
        /// </summary>
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            if (disposed)
            {
                if (!isDisposeMessageSent)
                {
                    lock (syncObject)
                    {
                        if (isDisposeMessageSent)
                        {
                            return ExportResult.Failure;
                        }

                        isDisposeMessageSent = true;
                    }
                }

                return ExportResult.Failure;
            }

            if (elmahIoClient == null)
            {
                var api = ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    WebProxy = options.WebProxy,
                    Timeout = new TimeSpan(0, 0, 30),
                    UserAgent = UserAgent(),
                });
                api.Messages.OnMessageFail += (sender, args) => options.OnError?.Invoke(args.Message, args.Error);
                elmahIoClient = api;
            }

            foreach (var logRecord in batch)
            {
                var baseException = logRecord.Exception?.GetBaseException();
                var createMessage = new CreateMessage
                {
                    DateTime = logRecord.Timestamp,
                    Detail = logRecord.Exception?.ToString(),
                    Type = baseException?.GetType().FullName,
                    Title = Title(logRecord),
                    Data = logRecord.Exception?.ToDataList() ?? [],
                    Severity = LogLevelToSeverity(logRecord.LogLevel),
                    Source = baseException?.Source,
                    Category = logRecord.CategoryName,
                    ServerVariables = [],
                    Cookies = [],
                    Form = [],
                    QueryString = [],
                };

                if (logRecord.Attributes != null)
                {
                    foreach (var attribute in logRecord.Attributes)
                    {
                        if (attribute.Key == OriginalFormatPropertyKey && attribute.Value is string value) createMessage.TitleTemplate = value;
                        else if (attribute.IsStatusCode(out int? statusCode)) createMessage.StatusCode = statusCode;
                        else if (attribute.IsApplication(out string? application)) createMessage.Application = application;
                        else if (attribute.IsSource(out string? source)) createMessage.Source = source;
                        else if (attribute.IsHostname(out string? hostname)) createMessage.Hostname = hostname;
                        else if (attribute.IsUser(out string? user)) createMessage.User = user;
                        else if (attribute.IsMethod(out string? method)) createMessage.Method = method;
                        else if (attribute.IsVersion(out string? version)) createMessage.Version = version;
                        else if (attribute.IsUrl(out string? url)) createMessage.Url = url;
                        else if (attribute.IsType(out string? type)) createMessage.Type = type;
                        else if (attribute.IsCorrelationId(out string? correlationId)) createMessage.CorrelationId = correlationId;
                        else if (attribute.IsCategory(out string? category)) createMessage.Category = category;
                        else if (attribute.IsRemoteAddr(out string? remoteAddr)) createMessage.ServerVariables.Add(new Item("Client-IP", remoteAddr));
                        else if (attribute.IsUserAgent(out string? userAgent)) createMessage.ServerVariables.Add(new Item("User-Agent", userAgent));
                        else if (attribute.IsServerVariables(out List<Item>? serverVariables)) serverVariables?.ForEach(sv => createMessage.ServerVariables.Add(sv));
                        else if (attribute.IsCookies(out List<Item>? cookies)) createMessage.Cookies = cookies;
                        else if (attribute.IsForm(out List<Item>? form)) createMessage.Form = form;
                        else if (attribute.IsQueryString(out List<Item>? queryString)) createMessage.QueryString = queryString;
                        else createMessage.Data.Add(attribute.ToItem());
                    }
                }

                var resource = ParentProvider.GetResource();
                if (resource != Resource.Empty)
                {
                    foreach (var attribute in resource.Attributes)
                    {
                        createMessage.Data.Add(new Item(attribute.Key, attribute.Value?.ToString()));
                    }
                }

                var eventId = logRecord.EventId;
                if (eventId != default)
                {
                    createMessage.Data.Add(new Item("EventId", eventId.Id.ToString()));
                    if (!string.IsNullOrWhiteSpace(eventId.Name)) createMessage.Data.Add(new Item("EventName", eventId.Name));
                }

                if (logRecord.TraceId != default)
                {
                    createMessage.Data.Add(new Item("TraceId", logRecord.TraceId.ToString()));
                    createMessage.CorrelationId = logRecord.TraceId.ToString();
                }

                if (logRecord.SpanId != default)
                {
                    createMessage.Data.Add(new Item("SpanId", logRecord.SpanId.ToString()));
                }

                if (logRecord.TraceFlags != default)
                {
                    createMessage.Data.Add(new Item("TraceFlags", logRecord.TraceFlags.ToString()));
                }

                logRecord.ForEachScope(ProcessScope, this);
                void ProcessScope(LogRecordScope scope, ElmahIoExporter exporter)
                {
                    foreach (var scopeItem in scope)
                    {
                        createMessage.Data.Add(new Item(scopeItem.Key, scopeItem.Value?.ToString()));
                    }
                }

                if (options.OnFilter != null && options.OnFilter(createMessage))
                {
                    continue;
                }

                options.OnMessage?.Invoke(createMessage);

                elmahIoClient.Messages.CreateAndNotify(options.LogId, createMessage);
            }

            return ExportResult.Success;
        }

        private static string Title(LogRecord message)
        {
            return message.FormattedMessage ?? message.Attributes?.ToString() ?? message.Exception?.GetBaseException()?.Message ?? "No message";
        }

        private static string LogLevelToSeverity(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Debug => Severity.Debug.ToString(),
                LogLevel.Error => Severity.Error.ToString(),
                LogLevel.Critical => Severity.Fatal.ToString(),
                LogLevel.Trace => Severity.Verbose.ToString(),
                LogLevel.Warning => Severity.Warning.ToString(),
                _ => Severity.Information.ToString(),
            };
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

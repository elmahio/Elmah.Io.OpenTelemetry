using Elmah.Io.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using OpenTelemetry.Logs;
using System.Diagnostics;
using System.Reflection;
using OT = OpenTelemetry;

namespace Elmah.Io.OpenTelemetry.Test
{
    public class ElmahIoExporterTest
    {
        [Test]
        public void CanExportBatch()
        {
            // Arrange
            var elmahIoClient = Substitute.For<IElmahioAPI>();
            var messagesClient = Substitute.For<IMessagesClient>();
            elmahIoClient.Messages.Returns(messagesClient);
            var options = new ElmahIoExporterOptions
            {
                ApiKey = Guid.NewGuid().ToString(),
                LogId = Guid.NewGuid(),
            };
            var exporter = new ElmahIoExporter(elmahIoClient, options);
            var batch = new OT.Batch<LogRecord>([CreateLogRecord()], 1);

            // Act
            var result = exporter.Export(batch);

            // Assert
            Assert.That(result, Is.EqualTo(OT.ExportResult.Success));
            messagesClient.Received(1).CreateAndNotify(Arg.Is(options.LogId), Arg.Is<CreateMessage>(msg =>
                msg.Title == "A message"
                && msg.DateTime.HasValue
                && msg.Detail != null && msg.Detail.Contains("System.Exception")
                && msg.Type == "System.Exception"
                && msg.Severity == "Warning"
                && msg.Category == "Category"));
        }

        private static LogRecord CreateLogRecord()
        {
            // Get the internal constructor using reflection
            var logRecordType = typeof(LogRecord);
            var constructor = logRecordType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

            // Create a LogRecord using the internal constructor
            var logRecord = (LogRecord)constructor.Invoke([]);
            logRecord.Timestamp = DateTime.UtcNow;
            logRecord.TraceId = new ActivityTraceId();
            logRecord.SpanId = new ActivitySpanId();
            logRecord.TraceFlags = new ActivityTraceFlags();
            logRecord.CategoryName = "Category";
            logRecord.LogLevel = LogLevel.Warning;
            logRecord.EventId = new EventId(1, "EventName");
            logRecord.FormattedMessage = "A message";
            logRecord.Exception = new Exception();

            return logRecord;
        }
    }
}

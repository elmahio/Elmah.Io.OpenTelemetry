using Elmah.Io.Client;
using System;
using System.Net;

namespace Elmah.Io.OpenTelemetry
{
    /// <summary>
    /// Contain properties for configuring the elmah.io exporter for OpenTelemetry.
    /// </summary>
    public class ElmahIoExporterOptions
    {
        /// <summary>
        /// The API key from the elmah.io UI.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// The id of the log to send messages to.
        /// </summary>
        public Guid LogId { get; set; }

        /// <summary>
        /// An application name to put on all error messages.
        /// </summary>
        public string? Application { get; set; }

        /// <summary>
        /// Register an action to be called before logging an error. Use the OnMessage action to
        /// decorate error messages with additional information.
        /// </summary>
        public Action<CreateMessage>? OnMessage { get; set; }

        /// <summary>
        /// Register an action to be called if communicating with the elmah.io API fails.
        /// You can use this callback to log the error through Microsoft.Extensions.Logging
        /// or what ever logging framework you may use.
        /// </summary>
        public Action<CreateMessage, Exception>? OnError { get; set; }

        /// <summary>
        /// Register an action to filter log messages. Use this to add client-side ignore
        /// of some error messages. If the filter action returns true, the error is ignored.
        /// </summary>
        public Func<CreateMessage, bool>? OnFilter { get; set; }

        /// <summary>
        /// If you need to access the internet through a web proxy from your server, you can use the
        /// <code>WebProxy</code> property to create a new instance of <code>System.Net.WebProxy</code>.
        /// </summary>
        public IWebProxy? WebProxy { get; set; }
    }
}

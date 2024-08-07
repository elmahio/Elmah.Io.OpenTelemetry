using OpenTelemetry;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Elmah.Io.OpenTelemetry
{
    internal static class UserAgentHelper
    {
        private static readonly string assemblyVersion = typeof(ElmahIoExporter).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        private static readonly string otAssemblyVersion = typeof(BaseExporter<>).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        internal static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.OpenTelemetry", assemblyVersion)).ToString())
                .Append(' ')
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("OpenTelemetry", otAssemblyVersion)).ToString())
                .ToString();
        }
    }
}

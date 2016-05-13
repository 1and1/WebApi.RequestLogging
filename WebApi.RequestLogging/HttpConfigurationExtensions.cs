using System.Web.Http;
using NLog;

namespace WebApi.RequestLogging
{
    public static class HttpConfigurationExtensions
    {
        /// <summary>
        /// Adds an instance of <see cref="RequestLoggingHandler"/> to <see cref="HttpConfiguration.MessageHandlers"/>.
        /// </summary>
        /// <param name="config">The Web API configuration to modify.</param>
        /// <param name="logger">The logger to write messages to. <c>null</c> to default to a logger named <c>ApiRequest</c>.</param>
        /// <param name="sensitiveDataIndicator">A case-insensitive string to look for in request and response bodies to indicate sensitive data that should not be logged. <c>null</c> to disable this feature.</param>
        public static HttpConfiguration EnableRequestLogging(this HttpConfiguration config, ILogger logger = null, string sensitiveDataIndicator = null)
        {
            config.MessageHandlers.Add(new RequestLoggingHandler());
            return config;
        }
    }
}

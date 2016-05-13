using System.Web.Http;
using NLog;

namespace WebApi.RequestLogging
{
    public static class HttpConfigurationExtensions
    {
        /// <summary>
        /// Enables request logging with a custom logger.
        /// </summary>
        /// <param name="config">The Web API configuration to modify.</param>
        /// <param name="logger">The logger to write messages to. <c>null</c> to default to a logger named <c>ApiRequest</c>.</param>
        /// <param name="sensitiveKeywords">A list of case-insensitive strings to look for in request and response bodies to indicate sensitive data that should not be logged.</param>
        public static HttpConfiguration EnableRequestLogging(this HttpConfiguration config, ILogger logger, params string[] sensitiveKeywords)
        {
            config.MessageHandlers.Add(new RequestLoggingHandler(logger, sensitiveKeywords));
            return config;
        }

        /// <summary>
        /// Enables request logging with the logger <c>ApiRequest</c>.
        /// </summary>
        /// <param name="config">The Web API configuration to modify.</param>
        /// <param name="sensitiveKeywords">A list of case-insensitive strings to look for in request and response bodies to indicate sensitive data that should not be logged.</param>
        public static HttpConfiguration EnableRequestLogging(this HttpConfiguration config, params string[] sensitiveKeywords)
        {
            return config.EnableRequestLogging(LogManager.GetLogger("ApiRequest"), sensitiveKeywords);
        }
    }
}

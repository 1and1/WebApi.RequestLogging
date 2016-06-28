using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace WebApi.RequestLogging
{
    public class RequestLoggingHandler : DelegatingHandler
    {
        private readonly ILogger _logger;
        private readonly string[] _sensitiveKeywords;

        /// <summary>
        /// Creates a new logging handler.
        /// </summary>
        /// <param name="logger">The logger to write messages to. <c>null</c> to default to a logger named <c>ApiRequest</c>.</param>
        /// <param name="sensitiveKeywords">A list of case-insensitive strings to look for in request and response bodies to indicate sensitive data that should not be logged.</param>
        public RequestLoggingHandler(ILogger logger, params string[] sensitiveKeywords)
        {
            _logger = logger;
            _sensitiveKeywords = sensitiveKeywords;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // NOTE: Capture HTTP method before passing through in case other handlers change it
            var method = request.Method;
            var response = await base.SendAsync(request, cancellationToken);
            var statusCode = response.StatusCode;

            var logLevel = GetLogLevel(statusCode, method);
            if (_logger.IsEnabled(logLevel))
            {
                var builder = new StringBuilder();
                builder.AppendLine(method + " " + request.RequestUri.PathAndQuery);

                var context = request.GetRequestContext();
                if (!string.IsNullOrEmpty(context.Principal?.Identity?.Name))
                    builder.AppendLine("User: " + context.Principal.Identity.Name);

                if (!string.IsNullOrEmpty(request.Headers.From))
                    builder.AppendLine("From: " + request.Headers.From);

                await AppendContentAsync(request.Content, builder, "Request");
                builder.AppendLine("Response code: " + statusCode);
                await AppendContentAsync(response.Content, builder, "Response");

                _logger.Log(logLevel, message: builder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
            }

            return response;
        }

        private static LogLevel GetLogLevel(HttpStatusCode statusCode, HttpMethod method)
        {
            // 2xx
            if (statusCode < (HttpStatusCode)300)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options || method == HttpMethod.Trace)
                    ? LogLevel.Debug
                    : LogLevel.Info;
            }

            // 3xx, 401
            if (statusCode < HttpStatusCode.BadRequest || statusCode == HttpStatusCode.Unauthorized)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options || method == HttpMethod.Trace)
                    ? LogLevel.Info
                    : LogLevel.Warn;
            }

            // 403, 404, 410
            if (statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.Gone)
            {
                if (method == HttpMethod.Head) return LogLevel.Info;
                return (method == HttpMethod.Get || method == HttpMethod.Options || method == HttpMethod.Trace)
                    ? LogLevel.Warn
                    : LogLevel.Error;
            }

            // 416
            if (statusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options || method == HttpMethod.Trace)
                    ? LogLevel.Info
                    : LogLevel.Error;
            }

            // Other 4xx
            if (statusCode < HttpStatusCode.InternalServerError)
                return LogLevel.Error;

            // 5xx
            return LogLevel.Fatal;
        }

        private async Task AppendContentAsync(HttpContent content, StringBuilder builder, string type)
        {
            string mediaType = content?.Headers.ContentType?.MediaType;
            if (string.IsNullOrEmpty(mediaType)) return;

            if (mediaType.StartsWith("text/") || mediaType.Contains("/xml") || mediaType.Contains("/json"))
            {
                string body = await content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    builder.AppendLine(ContainsSensitiveKeyword(body)
                        ? type + " body contains sensitive data"
                        : type + " body: " + body);
                }
            }
            else
                builder.AppendLine(type + " body has MIME type: " + mediaType);
        }

        private bool ContainsSensitiveKeyword(string body)
        {
            if (_sensitiveKeywords == null) return false;
            return _sensitiveKeywords.Any(x => body.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
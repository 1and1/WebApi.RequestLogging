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

            // NOTE: Buffer text content before passing through to avoid unseekable streams later on
            if (request.Content != null && IsXmlOrJson(request.Content))
                await request.Content.LoadIntoBufferAsync();

            var response = await base.SendAsync(request, cancellationToken);
            var statusCode = response.StatusCode;

            var logLevel = GetLogLevel(statusCode, method);
            if (_logger.IsEnabled(logLevel))
            {
                var builder = new StringBuilder();
                builder.AppendLine(method + " " + request.RequestUri.PathAndQuery);

                var range = request.Headers.Range?.Ranges.FirstOrDefault();
                if (range != null) builder.AppendLine("Range: " + range);

                var context = request.GetRequestContext();
                if (!string.IsNullOrEmpty(context.Principal?.Identity?.Name))
                    builder.AppendLine("User: " + context.Principal.Identity.Name);

                if (!string.IsNullOrEmpty(request.Headers.From))
                    builder.AppendLine("From: " + request.Headers.From);

                string requestBody = await ReadBodyAsync(request.Content);
                if (requestBody != null)
                    builder.AppendLine("Request body: " + requestBody);

                builder.AppendLine("Response code: " + statusCode);

                var responseBody = await ReadBodyAsync(response.Content);
                if (responseBody != null)
                    builder.AppendLine("Response body: " + responseBody);

                _logger.Log(logLevel, message: builder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
            }

            return response;
        }

        private async Task<string> ReadBodyAsync(HttpContent content)
        {
            if (content == null) return null;
            if (IsXmlOrJson(content))
            {
                var stream = await content.ReadAsStreamAsync();
                if (stream.CanSeek) stream.Position = 0;
                string body = await content.ReadAsStringAsync();
                if (stream.CanSeek) stream.Position = 0;

                if (string.IsNullOrEmpty(body))
                    return null;
                else if (_sensitiveKeywords.Any(x => body.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                    return "contains sensitive data";
                else return body;
            }
            else  return content.Headers.ContentType?.MediaType;
        }

        private static bool IsXmlOrJson(HttpContent content)
        {
            string type = content.Headers.ContentType?.MediaType;
            return type != null && (type.Contains("/xml") || type.Contains("/json"));
        }

        private static LogLevel GetLogLevel(HttpStatusCode statusCode, HttpMethod method)
        {
            // 1xx
            if (statusCode < HttpStatusCode.OK)
                return LogLevel.Trace;

            // 2xx
            if (statusCode < (HttpStatusCode)300)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options)
                    ? LogLevel.Debug
                    : LogLevel.Info;
            }

            // 3xx, 401
            if (statusCode < HttpStatusCode.BadRequest || statusCode == HttpStatusCode.Unauthorized)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options)
                    ? LogLevel.Info
                    : LogLevel.Warn;
            }

            // 403, 404, 410
            if (statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.Gone)
            {
                if (method == HttpMethod.Head) return LogLevel.Info;
                return (method == HttpMethod.Get || method == HttpMethod.Options || method == HttpMethod.Delete)
                    ? LogLevel.Warn
                    : LogLevel.Error;
            }

            // 416
            if (statusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                return (method == HttpMethod.Head || method == HttpMethod.Get || method == HttpMethod.Options)
                    ? LogLevel.Trace
                    : LogLevel.Error;
            }

            // Other 4xx
            if (statusCode < HttpStatusCode.InternalServerError)
                return LogLevel.Error;

            // 5xx
            return LogLevel.Fatal;
        }
    }
}
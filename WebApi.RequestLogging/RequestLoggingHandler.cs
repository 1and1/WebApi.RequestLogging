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
            var response = await base.SendAsync(request, cancellationToken);
            var level = GetLogLevel(request, response);

            if (_logger.IsEnabled(level))
            {
                var builder = new StringBuilder();
                builder.AppendLine(request.Method + " " + request.RequestUri.PathAndQuery);

                var context = request.GetRequestContext();
                if (!string.IsNullOrEmpty(context.Principal.Identity.Name))
                    builder.AppendLine("User: " + context.Principal.Identity.Name);

                if (!string.IsNullOrEmpty(request.Headers.From))
                    builder.AppendLine("From: " + request.Headers.From);

                await AppendContentAsync(request.Content, builder, "Request");
                builder.AppendLine("Response code: " + response.StatusCode);
                await AppendContentAsync(response.Content, builder, "Response");

                _logger.Log(level, message: builder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
            }

            return response;
        }

        private static LogLevel GetLogLevel(HttpRequestMessage request, HttpResponseMessage response)
        {
            bool safeMethod = (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head ||
                               request.Method == HttpMethod.Options || request.Method == HttpMethod.Trace);

            if (response.StatusCode >= HttpStatusCode.InternalServerError)
                return LogLevel.Fatal;

            return (response.StatusCode >= HttpStatusCode.BadRequest)
                ? (safeMethod ? LogLevel.Warn : LogLevel.Error)
                : (safeMethod ? LogLevel.Debug : LogLevel.Info);
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
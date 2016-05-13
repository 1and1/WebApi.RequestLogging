using System;
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
        private readonly string _sensitiveDataIndicator;

        /// <summary>
        /// Creates a new logging handler.
        /// </summary>
        /// <param name="logger">The logger to write messages to. <c>null</c> to default to a logger named <c>ApiRequest</c>.</param>
        /// <param name="sensitiveDataIndicator">A case-insensitive string to look for in request and response bodies to indicate sensitive data that should not be logged. <c>null</c> to disable this feature.</param>
        public RequestLoggingHandler(ILogger logger = null, string sensitiveDataIndicator = null)
        {
            _logger = logger ?? LogManager.GetLogger("ApiRequest");
            _sensitiveDataIndicator = sensitiveDataIndicator;
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

            return response.IsSuccessStatusCode
                ? (safeMethod ? LogLevel.Debug : LogLevel.Info)
                : (safeMethod ? LogLevel.Warn : LogLevel.Error);
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
                    builder.AppendLine(ContainsSensitiveData(body)
                        ? type + " body contains sensitive data"
                        : type + " body: " + body);
                }
            }
            else
                builder.AppendLine(type + " body has MIME type: " + mediaType);
        }

        private bool ContainsSensitiveData(string body)
        {
            if (_sensitiveDataIndicator == null) return false;
            return body.IndexOf(_sensitiveDataIndicator, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
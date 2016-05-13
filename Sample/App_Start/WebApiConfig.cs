using System.Web.Http;
using WebApi.RequestLogging;

namespace RequestLoggingSample
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.EnableRequestLogging(sensitiveKeywords: "secret");

            config.EnsureInitialized();
        }
    }
}
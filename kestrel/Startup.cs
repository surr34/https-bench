
namespace KestrelSample
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    internal class Response
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("schema")]
        public string Schema { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("headers")]
        public IHeaderDictionary Headers { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("query")]
        public IQueryCollection Query { get; set; }

        [JsonProperty("queryString")]
        public QueryString QueryString { get; set; }

        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
        public string Body { get; set; }
    }

    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var serverAddressesFeature =
                app.ServerFeatures.Get<IServerAddressesFeature>();

            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                if (context.Request.Query.TryGetValue("delay", out var delayStr))
                {
                    await Task.Delay(int.Parse(delayStr)).ConfigureAwait(false);
                }
                if (context.Request.Query.TryGetValue("echo", out var sizeStr))
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append('A', int.Parse(sizeStr));
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(stringBuilder.ToString());
                }
                else if (context.Request.Query.TryGetValue("base", out var _))
                {
                    await next.Invoke().ConfigureAwait(false);
                }
                else
                {
                    try
                    {
                        var response = new Response
                        {
                            Method = context.Request.Method,
                            Schema = context.Request.HttpContext.Request.Scheme,
                            Protocol = context.Request.HttpContext.Request.Protocol,
                            Host = context.Request.HttpContext.Request.Host.ToString(),
                            Path = context.Request.Path.ToString(),
                            Headers = context.Request.Headers,
                            Query = context.Request.Query,
                            QueryString = context.Request.QueryString,
                        };

                        context.Response.ContentType = "application/json";

                        if (context.Request.Body != null)
                        {
                            using (var reader = new StreamReader(context.Request.Body))
                            {
                                response.Body = await reader.ReadToEndAsync().ConfigureAwait(false);
                            }
                        }

                        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                    }
                    catch (Exception exception)
                    {
                        var result = new
                        {
                            error = exception.Message
                        };

                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                    }
                }
            });

            app.Run(async (context) =>
            {
                context.Response.ContentType = "text/html";
                await context.Response
                    .WriteAsync("<!DOCTYPE html><html lang=\"en\"><head>" +
                        "<title></title></head><body><p>Hosted by Kestrel</p>");

                if (serverAddressesFeature != null)
                {
                    await context.Response
                        .WriteAsync("<p>Listening on the following addresses: " +
                            string.Join(", ", serverAddressesFeature.Addresses) +
                            "</p>");
                }

                await context.Response.WriteAsync("<p>Request URL: " +
                    $"{context.Request.GetDisplayUrl()}</p>");
                await context.Response.WriteAsync("<p>Request URL: " +
                    $"{context.Request.Host}</p>");
            });
        }
    }
}

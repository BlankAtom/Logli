 // Copyright (C) 2017 Alaa Masoud
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Sejil.Configuration.Internal;
using Sejil.Routing.Internal;
using Sejil.Models.Internal;
using Serilog.Context;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace Sejil
{
    public static class ApplicationBuilderExtensions
    {
        internal static readonly JsonSerializerOptions _camelCaseJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Adds Sejil to the request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSejil(this IApplicationBuilder app)
        {
            var settings = app.ApplicationServices.GetService(typeof(ISejilSettings)) as SejilSettings;
            var url = settings.Url.Substring(1); // Skip the '/'

            app.Use(async (context, next) =>
            {
                var userName = context.User.Identity.IsAuthenticated
                    ? context.User.Identity.Name
                    : context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                using (LogContext.PushProperty("Username", userName))
                {
                    await next.Invoke();
                }
            });

            app.UseRouter(routes =>
            {
                routes.MapGet(url, async context =>
                {
                    var controller = GetSejilController(context);
                    await controller.GetIndexAsync();
                });

                routes.MapPost($"{url}/events", async context =>
                {
                    var query = await JsonSerializer.DeserializeAsync<LogQueryFilter>(context.Request.Body, _camelCaseJson);
                    int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var page);
                    var dateParsed = DateTime.TryParse(context.Request.Query["startingTs"].FirstOrDefault(), out var startingTs);

                    var controller = GetSejilController(context);
                    await controller.GetEventsAsync(page, dateParsed ? startingTs : (DateTime?)null, query);
                });

                routes.MapPost($"{url}/log-query", async context =>
                {
                    var logQuery = await JsonSerializer.DeserializeAsync<LogQuery>(context.Request.Body, _camelCaseJson);

                    var controller = GetSejilController(context);
                    await controller.SaveQueryAsync(logQuery);
                });

                routes.MapGet($"{url}/log-queries", async context =>
                {
                    var controller = GetSejilController(context);
                    await controller.GetQueriesAsync();
                });

                routes.MapGet($"{url}/min-log-level", async context =>
                {
                    var controller = GetSejilController(context);
                    await controller.GetMinimumLogLevelAsync();
                });

                routes.MapGet($"{url}/user-name", async context =>
                {
                    var controller = GetSejilController(context);
                    await controller.GetUserNameAsync();
                });

                routes.MapPost($"{url}/min-log-level", async context =>
                {
                    var minLogLevel = await GetRequestBodyAsync(context.Request);
                    var controller = GetSejilController(context);
                    controller.SetMinimumLogLevel(minLogLevel);
                });

                routes.MapPost($"{url}/del-query", async context =>
                {
                    var queryName = await GetRequestBodyAsync(context.Request);
                    var controller = GetSejilController(context);
                    await controller.DeleteQueryAsync(queryName);
                });

                routes.MapGet($"{url}/title", async context =>
                {
                    var controller = GetSejilController(context);
                    await controller.GetTitleAsync();
                });
            });

            return app;
        }

        private static ISejilController GetSejilController(HttpContext context)
            => context.RequestServices.GetService(typeof(ISejilController)) as ISejilController;

        private static async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            var length = request.ContentLength;

            if (length > 0)
            {
                var buffer = new byte[(int)length];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer);
            }

            return null;
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace PBIProxy
{
    public class Startup
    {
        static HttpClient _httpClient { get; set; }
        static string _geoCodeServerURL { get; set; }
        static string _tileServerURL { get; set; }
        static string _pbiTileServerURL { get; set; }

        public Startup(IConfiguration configuration)
        {
            _tileServerURL = configuration["TileServerURL"];
            _pbiTileServerURL = configuration["PBITileServerURL"];
            _geoCodeServerURL = configuration["GeoCodeServerURL"];

            var httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(httpClientHandler, false);
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Map("/usrsvcs", HandleGeoCodeServer);
            app.Map("/arcgis", HandleTileMapServer);

            app.Use(async (context, next) =>
            {
                // Query strings have been converted to static files trailing with an underscore and the filename
                if (context.Request.QueryString.Value != "")
                {
                    context.Request.Path = string.Format("{0}_{1}", context.Request.Path.Value, context.Request.QueryString.Value.Substring(1));
                }
                await next();
            });

            // for each of the external folders make them accessible in the root folder
            var externalFolder = Path.Combine(Directory.GetCurrentDirectory(), "PBIStaticFiles");
            foreach (var folder in Directory.EnumerateDirectories(externalFolder))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx => {
                        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "ms-pbi://pbi.microsoft.com");
                    },
                    FileProvider = new PhysicalFileProvider(folder),
                    RequestPath = string.Format("/{0}", new DirectoryInfo(folder).Name),
                    ServeUnknownFileTypes = true,
                    DefaultContentType = "application/json"
                });
            }
        }

        private static void HandleGeoCodeServer(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                // Translate from the Request.From to a KeyValuePair collection
                var formData = new List<KeyValuePair<string, string>>();
                foreach (var k in context.Request.Form.Keys)
                {
                    formData.Add(new KeyValuePair<string, string>(k, context.Request.Form[k]));
                }

                // fetch the response from the configured source
                var postContent = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(_geoCodeServerURL, postContent);

                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "ms-pbi://pbi.microsoft.com");
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                    await context.Response.WriteAsync(content);
                }
                else
                {
                    await context.Response.WriteAsync("An error occurred: " + content);
                }
            });
        }

        private static void HandleTileMapServer(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                // fetch the response from the configured source
                var ExtraURL = context.Request.Path.Value.Replace(_pbiTileServerURL, "");
                var requestURL = $"{_tileServerURL}{ExtraURL}{context.Request.QueryString.Value}";
                var response = await _httpClient.GetAsync(requestURL);

                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "ms-pbi://pbi.microsoft.com");
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                    context.Response.Headers.Add("Cache-Control", response.Headers.CacheControl.ToString());
                    context.Response.Headers.Add("Last-Modified", response.Content.Headers.LastModified.ToString());
                    await context.Response.WriteAsync(content);
                }
                else
                {
                    await context.Response.WriteAsync("An error occurred: " + content);
                }
            });
        }
    }
}

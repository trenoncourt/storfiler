using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Storfiler.Constants;
using Storfiler.Dtos;
using Storfiler.Extensions;
using Storfiler.Options;
using Storfiler.Core;

namespace Storfiler
{
    public class Program
    {
        private static ApplicationOptions _appOptions;

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            try
            {
                Log.Information($"Starting Web host at {Environment.GetEnvironmentVariable("ASPNETCORE_URLS")} with env {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                IWebHostBuilder webHostBuilder = CreateWebHostBuilder();
                webHostBuilder.Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder()
        {
            IWebHostBuilder builder = new WebHostBuilder();
            _appOptions = Configuration.Get<ApplicationOptions>();
            Log.Debug("Configuration used: {@configuration}", _appOptions);

            if (_appOptions.Host.UseIis)
            {
                builder.UseIISIntegration();
            }
            builder
                .SuppressStatusMessages(true)
                .UseKestrel((options) => Configuration.GetSection(nameof(ApplicationOptions.Kestrel)))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(Configuration)
                .ConfigureServices(services => services.AddCors().AddRouting())
                .UseDefaultServiceProvider((context, options) => options.ValidateScopes = context.HostingEnvironment.IsDevelopment())
                .Configure(ConfigureApp);

            return builder;
        }

        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            app.UseRouter(builder =>
            {
                foreach (var s in _appOptions.Storfiler)
                {
                    foreach (var method in s.Methods)
                    {
                        builder.MapVerb(method.Verb.ToUpper(), $"{s.Resource}/{method.Path.TrimStart('/')}", async c =>
                        {
                            Log.Debug("Request: {@ip} -> {@method} {@protocol} {@scheme}://{@host}{@path}{@queryString}", c.Connection.RemoteIpAddress.ToString(), c.Request.Method, c.Request.Protocol, c.Request.Scheme, c.Request.Host.ToString(), c.Request.Path.ToString(), c.Request.QueryString.ToString());
                            try
                            {
                                switch (method.Action)
                                {
                                    case StorfilerActions.List:
                                        await StorfilerAction.ListFilesAsync(c, s);
                                        break;
                                    case StorfilerActions.Download:
                                        await StorfilerAction.DownloadFileAsync(c, method, s);
                                        break;
                                    case StorfilerActions.Add:
                                        StorfilerAction.AddFile(c, s);
                                        break;
                                    case StorfilerActions.Remove:
                                        StorfilerAction.RemoveFile(c, s);
                                        break;
                                    case StorfilerActions.Search:
                                        await StorfilerAction.SearchFilesAsync(c, method, s);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Fatal(e, e.Message);
                            }
                        });
                    }
                }
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Storfiler.Constants;
using Storfiler.Extensions;
using Storfiler.Options;

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
                Log.Information("Starting web host");
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
            IWebHostBuilder builder =  new WebHostBuilder();
            _appOptions = Configuration.Get<ApplicationOptions>();
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
                        builder.MapVerb(method.Verb.ToUpper(), $"{s.Resource}/{method.Path.TrimStart('/')}", async context =>
                        {
                            switch (method.Action)
                            {
                                case StorefilerAction.List:
                                    IEnumerable<string> files = Directory.EnumerateFiles(s.DiskPaths.Read.First(), "*", SearchOption.AllDirectories);
                                    await context.WriteJsonAsync(files);
                                    break;
                                case StorefilerAction.Find:
                                    try
                                    {
                                        FileStream fileStream = File.OpenRead(Path.Combine(s.DiskPaths.Read.First(), context.GetRouteValue("fileName").ToString()));
                                        context.Response.ContentType = "application/octet-stream";
                                        await fileStream.CopyToAsync(context.Response.Body);
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Fatal(e, e.Message);
                                    }
                                    break;
                                case StorefilerAction.Add:
                                    break;
                                case StorefilerAction.Remove:
                                    break;
                            }
                        });
                    } 
                }
            });
        }
    }
}
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Storfiler.Options;

namespace Storfiler
{
    public class Program
    {
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
            var appOptions = Configuration.Get<ApplicationOptions>();
            if (appOptions.Host.UseIis)
            {
                builder.UseIISIntegration();
            }
            builder
                .SuppressStatusMessages(true)
                .UseKestrel((options) => Configuration.GetSection(nameof(ApplicationOptions.Kestrel)))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(Configuration)
                .ConfigureServices(services => services.AddCors())
                .UseDefaultServiceProvider((context, options) => options.ValidateScopes = context.HostingEnvironment.IsDevelopment())
                .Configure(ConfigureApp);
            
            return builder;
        }

        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            app.Run(async (context) => { await context.Response.WriteAsync("Hello World!"); });
        }
    }
}
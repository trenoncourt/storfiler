using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Storfiler.AspNetCore.Extensions;
using Storfiler.AspNetCore.Extensions.DependencyInjection;
using Storfiler.Options;

namespace Storfiler
{
    public class Program
    {
        private static ApplicationOptions _appOptions;

        public static IConfiguration Configuration => new ConfigurationBuilder()
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
                .ConfigureServices(services => services.AddCors().AddStorfiler(Configuration.GetSection("Storfiler")))
                .UseDefaultServiceProvider((context, options) => options.ValidateScopes = context.HostingEnvironment.IsDevelopment())
                .Configure(ConfigureApp);

            return builder;
        }

        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            app.UseStorfiler();
        }
    }
}
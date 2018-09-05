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
            IWebHostBuilder builder =  new WebHostBuilder();
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
                                string filePath;
                                switch (method.Action)
                                {
                                    case StorefilerAction.List:
                                        Log.Information("Query: Read directory files {directory}", s.DiskPaths.Read.First());
                                        IEnumerable<string> listFiles = Directory.EnumerateFiles(s.DiskPaths.Read.First(), "*", SearchOption.AllDirectories);
                                        await c.WriteJsonAsync(listFiles);
                                        break;
                                    case StorefilerAction.Find:
                                        filePath = Path.Combine(s.DiskPaths.Read.First(), c.GetRouteValue("fileName").ToString());
                                        Log.Information("Query: Read file {file}", filePath);
                                        if (!File.Exists(filePath))
                                        {
                                            c.Response.StatusCode = StatusCodes.Status404NotFound;
                                            Log.Warning("File {file} does not exist", filePath);
                                            return;
                                        }
                                        FileStream fileStream = File.OpenRead(filePath);
                                        c.Response.ContentType = "application/octet-stream";
                                        await fileStream.CopyToAsync(c.Response.Body);
                                        break;
                                    case StorefilerAction.Add:
                                        break;
                                    case StorefilerAction.Remove:
                                        filePath = Path.Combine(s.DiskPaths.Read.First(), c.GetRouteValue("fileName").ToString());
                                        Log.Information("Command: Remove file {file}", filePath);
                                        if (!File.Exists(filePath))
                                        {
                                            c.Response.StatusCode = StatusCodes.Status404NotFound;
                                            Log.Warning("File {file} does not exist", filePath);
                                            return;
                                        }
                                        File.Delete(filePath);
                                        c.Response.StatusCode = StatusCodes.Status204NoContent;
                                        break;
                                    case StorefilerAction.Search:
                                        string fileName = c.GetRouteValue("fileName").ToString();
                                        string pattern = string.IsNullOrEmpty(method.Pattern) ? fileName : method.Pattern.Replace("{fileName}", fileName);
                                        Log.Information("Query: Search files in directory {directory} with pattern {pattern}", s.DiskPaths.Read.First(), pattern);
                                        IEnumerable<string> searchFiles = Directory.EnumerateFiles(s.DiskPaths.Read.First(), pattern, SearchOption.AllDirectories);
                                        if (method.AddMetadatas)
                                        {
                                            List<FileMetadatas> filesMetadatas = new List<FileMetadatas>();
                                            foreach (string file in searchFiles)
                                            {
                                                string name = Path.GetFileName(file);
                                                string parent = Path.GetFileName(Path.GetDirectoryName(file));
                                                filesMetadatas.Add(new FileMetadatas {FullPath = file, Name = name, Parent = parent});
                                            }
                                            await c.WriteJsonAsync(filesMetadatas);
                                            return;
                                        }
                                        await c.WriteJsonAsync(searchFiles);
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
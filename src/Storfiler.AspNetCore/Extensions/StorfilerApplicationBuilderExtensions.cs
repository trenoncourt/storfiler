using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Storage.Net.Blob;
using Storfiler.AspNetCore.Constants;
using Storfiler.AspNetCore.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Storfiler.AspNetCore.Extensions
{
    public static class StorfilerApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseStorfiler(this IApplicationBuilder builder)
        {
            var settings = builder.ApplicationServices.GetService<IOptions<StorfilerOptions>>().Value;
            var loggerFactory = builder.ApplicationServices.GetService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("StorfilerApplicationBuilderExtensions");


            return builder.Map(settings.Path == null ? null : $"/{settings.Path.TrimStart('/')}", app =>
            {
                app.UseRouter(router =>
                {
                    foreach (var s in settings.Storages)
                    {
                        foreach (var method in s.Methods)
                        {
                            router.MapVerb(method.Verb.ToUpper(), $"{s.Resource}/{method.Path.TrimStart('/')}",
                                async c =>
                                {
                                    logger.LogDebug(
                                        "Request: {@ip} -> {@method} {@protocol} {@scheme}://{@host}{@path}{@queryString}",
                                        c.Connection.RemoteIpAddress.ToString(), c.Request.Method, c.Request.Protocol,
                                        c.Request.Scheme, c.Request.Host.ToString(), c.Request.Path.ToString(),
                                        c.Request.QueryString.ToString());
                                    var storfilerService = c.RequestServices.GetService<StorfilerService>();
                                    try
                                    {
                                        switch (method.Action)
                                        {
                                            case StorfilerActions.List:
                                                var files = await storfilerService.ListFilesAsync(method, s);
                                                await c.WriteJsonAsync(files);
                                                break;
                                            case StorfilerActions.Download:
                                                if (string.IsNullOrEmpty(method.Query) ||
                                                    !c.Request.Query.ContainsKey(method.Query))
                                                {
                                                    logger.LogWarning("Could not find file in route or query {query}",
                                                        method.Query);
                                                    c.Response.StatusCode = StatusCodes.Status400BadRequest;
                                                    return;
                                                }

                                                string downloadFilePath = c.Request.Query[method.Query];
                                                Stream fileStream = null;
                                                try
                                                {
                                                    fileStream =
                                                        await storfilerService.DownloadFileAsync(method, s,
                                                            downloadFilePath);
                                                    if (fileStream == null)
                                                    {
                                                        c.Response.StatusCode = StatusCodes.Status404NotFound;
                                                        return;
                                                    }

                                                    string contentType = MimeTypeMap.List.MimeTypeMap
                                                        .GetMimeType(Path.GetExtension(downloadFilePath)).First();
                                                    c.Response.Headers.Add("Content-Disposition",
                                                        $"attachment; filename=\"{Path.GetFileName(downloadFilePath)}\"");
                                                    c.Response.ContentType = contentType;
                                                    await fileStream.CopyToAsync(c.Response.Body);
                                                }
                                                finally
                                                {
                                                    fileStream?.Dispose();
                                                }

                                                break;
                                            case StorfilerActions.Add:
                                                if (!c.Request.HasFormContentType)
                                                {
                                                    c.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                                                    return;
                                                }

                                                if (!c.Request.Form.Files.Any())
                                                {
                                                    Log.Warning("The request form does not contain a file");
                                                    c.Response.StatusCode = StatusCodes.Status400BadRequest;
                                                    return;
                                                }
//
//                                                if (c.Request.Form.Keys.Contains(method.Query))
//                                                {
//                                                    Log.Warning("The request form does not contain {query} part",
//                                                        method.Query);
//                                                    c.Response.StatusCode = StatusCodes.Status400BadRequest;
//                                                    return;
//                                                }

                                                await storfilerService.AddFileAsync(method, s, c.Request.Form.Files,
                                                    c.Request.Form[method.Query].ToString());
                                                break;
                                            case StorfilerActions.Remove:
                                                if (string.IsNullOrEmpty(method.Query) ||
                                                    !c.Request.Query.ContainsKey(method.Query))
                                                {
                                                    logger.LogWarning("Could not find file in route or query {query}",
                                                        method.Query);
                                                    c.Response.StatusCode = StatusCodes.Status400BadRequest;
                                                    return;
                                                }

                                                string deleteFilePath = c.Request.Query[method.Query];
                                                await storfilerService.DeleteFileAsync(method, s, deleteFilePath);
                                                break;
                                            case StorfilerActions.Search:
                                                if (string.IsNullOrEmpty(method.Query) ||
                                                    !c.Request.Query.ContainsKey(method.Query))
                                                {
                                                    logger.LogWarning("Could not find file in route or query {query}",
                                                        method.Query);
                                                    c.Response.StatusCode = StatusCodes.Status400BadRequest;
                                                    return;
                                                }

                                                string searchFilePath = c.Request.Query[method.Query];
                                                var searchFiles = await storfilerService.SearchFilesAsync(method, s, searchFilePath);
                                                await c.WriteJsonAsync(searchFiles);
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        logger.LogError(e, e.Message);
                                    }
                                });
                        }
                    }
                });
            });
        }
    }
}
using Microsoft.AspNetCore.Http;
using Serilog;
using Storfiler.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Storfiler.Extensions;
using Microsoft.AspNetCore.Routing;
using Storfiler.Dtos;

namespace Storfiler.Core
{
    public class StorfilerAction
    {
        public static async Task ListFilesAsync(HttpContext ctx, StorfilerOptions options)
        {
            Log.Information("Query: Read directory files {directory}", options.DiskPaths.Read.First());
            IEnumerable<string> listFiles = Directory.EnumerateFiles(options.DiskPaths.Read.First(), "*", SearchOption.AllDirectories);
            await ctx.WriteJsonAsync(listFiles);
        }

        public static async Task DownloadFileAsync(HttpContext ctx, StorfilerMethodOptions method, StorfilerOptions options)
        {
            string filePath = null;
            if (!string.IsNullOrEmpty(method.Query) && ctx.Request.Query.ContainsKey(method.Query))
            {
                filePath = ctx.Request.Query[method.Query];
            }
            else
            {
                filePath = ctx.GetRouteValue("fileName")?.ToString();
                if (string.IsNullOrEmpty(filePath))
                {
                    Log.Warning("Could not find file in route or query", filePath);
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            if (!method.IsFullPath)
            {
                filePath = Path.Combine(options.DiskPaths.Read.First(), filePath);
            }
            Log.Information("Query: Read file {file}", filePath);
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                Log.Warning("File {file} does not exist", filePath);
                return;
            }

            using (FileStream fileStream = File.OpenRead(filePath))
            {
                string contentType = MimeTypeMap.List.MimeTypeMap.GetMimeType(Path.GetExtension(filePath)).First();
                ctx.Response.Headers.TryAdd("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(filePath)}\"");
                ctx.Response.ContentType = contentType;
                await fileStream.CopyToAsync(ctx.Response.Body);
            }
        }

        public static void AddFile(HttpContext ctx, StorfilerOptions options)
        {
            var files = ctx.Request.Form.Files;
            if (files == null || !files.Any())
            {
                Log.Warning("The request form does not contain a file");
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(file.FileName))
                {
                    Log.Warning("Attempt to upload a file with a null or empty name");
                    continue;
                }

                try
                {
                    var stream = file.OpenReadStream();
                    var buffer = new byte[file.Length];

                    stream.ReadAsync(buffer, 0, buffer.Length);

                    string subPath = ctx.Request.Form["Path"];

                    var filePath = subPath != null ?
                        Path.Combine(options.DiskPaths.Write, subPath, file.FileName) :
                        Path.Combine(options.DiskPaths.Write, file.FileName);

                    var fileStream = File.Create(filePath);
                    fileStream.Write(buffer, 0, buffer.Length);

                    fileStream.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, ex.Message);
                }
            }
        }

        public static void RemoveFile(HttpContext ctx, StorfilerOptions options)
        {
            string filePath = Path.Combine(options.DiskPaths.Read.First(), ctx.GetRouteValue("fileName").ToString());
            Log.Information("Command: Remove file {file}", filePath);
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                Log.Warning("File {file} does not exist", filePath);
                return;
            }
            File.Delete(filePath);
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        }

        public static async Task SearchFilesAsync(HttpContext ctx, StorfilerMethodOptions method, StorfilerOptions options)
        {
            string fileName = ctx.GetRouteValue("fileName").ToString();
            string pattern = string.IsNullOrEmpty(method.Pattern) ? fileName : method.Pattern.Replace("{fileName}", fileName);
            Log.Information("Query: Search files in directory {directory} with pattern {pattern}", options.DiskPaths.Read.First(), pattern);
            IEnumerable<string> searchFiles = Directory.EnumerateFiles(options.DiskPaths.Read.First(), pattern, SearchOption.AllDirectories);
            List<FileMetadatas> filesMetadatas = new List<FileMetadatas>();
            foreach (string file in searchFiles)
            {
                string name = Path.GetFileName(file);
                string nameWithoutExt = name.Remove(name.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase));
                string parent = Path.GetFileName(Path.GetDirectoryName(file));
                filesMetadatas.Add(new FileMetadatas
                {
                    FullPath = file,
                    Name = name,
                    Parent = parent,
                    NameWithoutExt = nameWithoutExt
                });
            }
            await ctx.WriteJsonAsync(filesMetadatas);
        }
    }
}

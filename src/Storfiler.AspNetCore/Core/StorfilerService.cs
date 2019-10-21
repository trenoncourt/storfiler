using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storage.Net.Blob;
using Storfiler.AspNetCore.Dtos;
using Storfiler.AspNetCore.Extensions.DependencyInjection;

namespace Storfiler.AspNetCore.Core
{
    public class StorfilerService
    {
        private readonly IStorageProvider _storageProvider;
        private readonly ILogger<StorfilerService> _logger;

        public StorfilerService(IStorageProvider storageProvider, ILogger<StorfilerService> logger)
        {
            _storageProvider = storageProvider;
            _logger = logger;
        }
        
        public async Task<ICollection<FileMetadatas>> ListFilesAsync(MethodOptions methodOptions, StorageOptions storageOptions, ListOptions listOptions = null)
        {
            ICollection<EndpointOptions> endpoints = GetReadEndpoints(methodOptions, storageOptions).ToList();
            _logger.LogInformation("List files in directories {@directory}", endpoints.Select(e => e.Path));
            IEnumerable<IBlobStorage> storages = endpoints.Select(e => _storageProvider.GetStorage(e.Provider));
            IEnumerable<Task<IReadOnlyCollection<BlobId>>> readTasks = storages.Select(x => x.ListAsync(listOptions));
            IEnumerable<BlobId> blobs = (await Task.WhenAll(readTasks)).SelectMany(x => x.Select(t => t));
            _logger.LogDebug("Listed files {@blobs}", blobs);
            return blobs.Select(x => x.ToFileMetadata()).ToList();
        }

        public async Task<ICollection<FileMetadatas>> SearchFilesAsync(MethodOptions methodOptions, StorageOptions storageOptions, string fileName)
        {
            string pattern = string.IsNullOrEmpty(methodOptions.Pattern) ? fileName : methodOptions.Pattern.Replace("{fileName}", fileName);
            ICollection<EndpointOptions> endpoints = GetReadEndpoints(methodOptions, storageOptions).ToList();
            _logger.LogInformation("Query: Search files in directory {@directory} with pattern {pattern}", endpoints.Select(e => e.Path), pattern);
            IDictionary<EndpointOptions, IBlobStorage> storages = endpoints.ToDictionary(e => e, e => _storageProvider.GetStorage(e.Provider));
            List<FileMetadatas> filesMetadatas = new List<FileMetadatas>();
            List<Task<IReadOnlyCollection<BlobId>>> storageTasks = new List<Task<IReadOnlyCollection<BlobId>>>();
            foreach (KeyValuePair<EndpointOptions,IBlobStorage> storage in storages)
            {
                if (storage.Key.Provider.StorageType == StorageType.Directory && storage.Key.IsRegex)
                {
                    IEnumerable<string> searchFiles = Directory.EnumerateFiles(storage.Key.Provider.Directory, pattern, SearchOption.AllDirectories);
                    
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
                }
                else
                {
                    storageTasks.Add(storage.Value.ListAsync(new ListOptions { Recurse = true }));
                }
            }

            var taskResults = await Task.WhenAll(storageTasks);
            foreach (var taskResult in taskResults)
            {
                filesMetadatas.AddRange(taskResult.Select(x => x.ToFileMetadata()));
            }

            return filesMetadatas;
        }

        public async Task<Stream> DownloadFileAsync(MethodOptions methodOptions, StorageOptions storageOptions, string filePath)
        {
            EndpointOptions endpoint = FindReadEndpoint(methodOptions, storageOptions);
            if (!endpoint.Provider.IsFullPath)
            {
                filePath = Path.Combine(endpoint.Path ?? "", filePath);
                Log.Information("Query: Download file {file}", filePath);
                string fileName = Path.GetFileName(filePath);
                IBlobStorage storage = _storageProvider.GetStorage(endpoint.Provider, filePath.Replace(fileName, ""));
                bool fileExist = await storage.ExistsAsync(fileName);
                if (!fileExist) return null;
                return await storage.OpenReadAsync(fileName);
            }
            else
            {
                Log.Information("Query: Download file {file}", filePath);
                IBlobStorage storage = _storageProvider.GetStorage(endpoint.Provider);
                bool fileExist = await storage.ExistsAsync(filePath);
                if (!fileExist) return null;
                return await storage.OpenReadAsync(filePath);
            }                       
        }

        public async Task AddFileAsync(MethodOptions methodOptions, StorageOptions storageOptions, IFormFileCollection files, string subPath)
        {
            ICollection<EndpointOptions> endpoints = GetWriteEndpoints(methodOptions, storageOptions).ToList();
            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(file.FileName))
                {
                    Log.Warning("Attempt to upload a file with a null or empty name");
                    continue;
                }
                try
                {
                    foreach (EndpointOptions endpoint in endpoints)
                    {
                        IBlobStorage storage = _storageProvider.GetStorage(endpoint.Provider);
                        using (var fs = file.OpenReadStream())
                        {
                            await storage.WriteAsync(file.FileName, fs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                }
            }
        }

        public async Task<bool> DeleteFileAsync(MethodOptions methodOptions, StorageOptions storageOptions, string filePath)
        {
            EndpointOptions endpoint = FindWriteEndpoint(methodOptions, storageOptions);
            if (!endpoint.Provider.IsFullPath)
            {
                filePath = Path.Combine(endpoint.Path ?? "", filePath);
                string fileName = Path.GetFileName(filePath);
                Log.Information("Command: Remove file {file}", filePath);
                IBlobStorage storage = _storageProvider.GetStorage(endpoint.Provider, filePath.Replace(fileName, ""));
                bool fileExist = await storage.ExistsAsync(fileName);
                if (!fileExist) return false;
                await storage.DeleteAsync(fileName);
            }
            else
            {
                IBlobStorage storage = _storageProvider.GetStorage(endpoint.Provider);
                Log.Information("Command: Remove file {file}", filePath);
                bool fileExist = await storage.ExistsAsync(filePath);
                if (!fileExist) return false;
                await storage.DeleteAsync(filePath);
            }

            return true;
        }

        private EndpointOptions FindReadEndpoint(MethodOptions methodOptions, StorageOptions storageOptions)
        {
            return methodOptions?.ReadEndpoint ?? methodOptions?.Endpoint ?? storageOptions?.ReadEndpoint ?? storageOptions?.Endpoint;
        }

        private EndpointOptions FindWriteEndpoint(MethodOptions methodOptions, StorageOptions storageOptions)
        {
            return methodOptions?.WriteEndpoint ?? methodOptions?.Endpoint ?? storageOptions?.WriteEndpoint ?? storageOptions?.Endpoint;
        }

        private IEnumerable<EndpointOptions> GetReadEndpoints(MethodOptions methodOptions, StorageOptions storageOptions)
        {
            IEnumerable<EndpointOptions> endpoints;
            if (methodOptions?.ReadEndpoints != null && storageOptions?.ReadEndpoints != null)
            {
                endpoints = methodOptions?.ReadEndpoints ?? storageOptions?.ReadEndpoints;
            }
            else
            {
                endpoints = new List<EndpointOptions> {methodOptions?.ReadEndpoint ?? methodOptions?.Endpoint ?? storageOptions?.ReadEndpoint ?? storageOptions?.Endpoint};
            }
            return endpoints;
        }

        private IEnumerable<EndpointOptions> GetWriteEndpoints(MethodOptions methodOptions, StorageOptions storageOptions)
        {
            IEnumerable<EndpointOptions> endpoints;
            if (methodOptions?.WriteEndpoints != null && storageOptions?.WriteEndpoints != null)
            {
                endpoints = methodOptions?.WriteEndpoints ?? storageOptions?.WriteEndpoints;
            }
            else
            {
                endpoints = new List<EndpointOptions> {methodOptions?.WriteEndpoint ?? methodOptions?.Endpoint ?? storageOptions?.WriteEndpoint ?? storageOptions?.Endpoint};
            }
            return endpoints;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Storfiler.AspNetCore
{
    public class StorfilerOptions
    {
        public string Path { get; set; }

        public IEnumerable<StorageOptions> Storages { get; set; }
    }

    public class StorageOptions
    {
        public string Resource { get; set; }

        public ProviderOptions Provider { get; set; }

        public IEnumerable<EndpointOptions> Endpoints { get; set; }
        
        public EndpointOptions Endpoint { get; set; }
        
        public IEnumerable<EndpointOptions> ReadEndpoints { get; set; }
        
        public EndpointOptions ReadEndpoint { get; set; }
        
        public IEnumerable<EndpointOptions> WriteEndpoints { get; set; }
        
        public EndpointOptions WriteEndpoint { get; set; }

        public IEnumerable<MethodOptions> Methods { get; set; }
    }

    public class EndpointOptions
    {
        public string Path { get; set; }

        public bool IsRegex { get; set; }

        public ProviderOptions Provider { get; set; }
    }

    public class AzureOptions
    {
        public string AzureStorageAccountName { get; set; }
        
        public string AzureStorageKey { get; set; }
        
        public string AzureStorageContainerName { get; set; }
    }
    
    public class ProviderOptions
    {
        public AzureOptions Azure { get; set; }

        public string Type { get; set; }

        public string Directory { get; set; }

        public bool IsFullPath { get; set; }
        
        public StorageType StorageType
        {
            get
            {
                if (Type?.Equals(StorageType.Directory.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    return StorageType.Directory;
                }
                if (Type?.Equals(StorageType.Azure.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    return StorageType.Azure;
                }

                // default type will be directory
                return StorageType.Directory;
            }
        }
    }

    public class MethodOptions
    {
        public string Verb { get; set; }

        public string Path { get; set; }

        public string Action { get; set; }

        public string Pattern { get; set; }

        public string Query { get; set; }
        
        public EndpointOptions Endpoint { get; set; }

        public IEnumerable<EndpointOptions> ReadEndpoints { get; set; }
        
        public EndpointOptions ReadEndpoint { get; set; }
        
        public IEnumerable<EndpointOptions> WriteEndpoints { get; set; }
        
        public EndpointOptions WriteEndpoint { get; set; }
    }

    public enum StorageType
    {
        Directory,
        Azure
    }
}
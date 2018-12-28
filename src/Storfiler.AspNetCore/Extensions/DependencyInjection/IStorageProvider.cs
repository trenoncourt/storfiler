using System;
using System.Collections.Generic;
using System.IO;
using Storage.Net;
using Storage.Net.Blob;

namespace Storfiler.AspNetCore.Extensions.DependencyInjection
{
    public interface IStorageProvider
    {
        IBlobStorage GetStorage(ProviderOptions providerOptions, string filePath = null);        
    }

    class StorageProvider : IStorageProvider
    {
        public IBlobStorage GetStorage(ProviderOptions providerOptions, string filePath = null)
        {
            switch (providerOptions.StorageType)
            {
                case StorageType.Directory:
                    return StorageFactory.Blobs.DirectoryFiles(new DirectoryInfo(filePath ?? providerOptions.Directory));
                case StorageType.Azure:
                    return StorageFactory.Blobs.AzureBlobStorage(providerOptions.Azure.AzureStorageAccountName, providerOptions.Azure.AzureStorageKey, providerOptions.Azure.AzureStorageContainerName);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
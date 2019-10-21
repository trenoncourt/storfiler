using System;
using System.IO;
using Storage.Net.Blob;

namespace Storfiler.AspNetCore.Dtos
{
    public class FileMetadatas
    {
        public string Name { get; set; }
        
        public string NameWithoutExt { get; set; }

        public string FullPath { get; set; }

        public string FolderPath { get; set; }

        public string Parent { get; set; }
    }

    public static class BlobIdExtensions
    {
        public static FileMetadatas ToFileMetadata(this BlobId blobId)
        {
            string nameWithoutExt = blobId.FullPath.Remove(blobId.FullPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase));
            string parent = Path.GetFileName(Path.GetDirectoryName(blobId.FullPath));
            return new FileMetadatas
            {
                Name = blobId.Id,
                Parent = parent,
                FullPath = blobId.FullPath,
                NameWithoutExt = nameWithoutExt,
                FolderPath = blobId.FolderPath
            };
        }
    }
}
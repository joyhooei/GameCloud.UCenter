﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace GF.UCenter.Common
{
    [Export]
    public class StorageAccountContext
    {
        private readonly CloudBlobContainer container;

        [ImportingConstructor]
        public StorageAccountContext(Settings settings)
        {
            var account = CloudStorageAccount.Parse(settings.UCStorageConnectionString);
            var client = account.CreateCloudBlobClient();
            this.container = client.GetContainerReference(settings.ImageContainerName);
        }

        public async Task<string> UploadBlobAsync(string blobName, Stream stream, CancellationToken token)
        {
            var blob = this.container.GetBlockBlobReference(blobName);
            await blob.UploadFromStreamAsync(stream, token);

            return blob.Uri.AbsoluteUri;
        }

        public async Task<string> CopyBlobAsync(string sourceBlobName, string targetBlobName, CancellationToken token)
        {
            var sourceBlob = this.container.GetBlockBlobReference(sourceBlobName);
            var targetBlob = this.container.GetBlockBlobReference(targetBlobName);
            await targetBlob.StartCopyAsync(sourceBlob, token);

            return targetBlob.Uri.AbsoluteUri;
        }
    }
}
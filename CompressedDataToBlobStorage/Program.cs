using Azure.Storage.Blobs;
using CompressedDataToBlobStorage;
using System.IO.Compression;

var secretAppsettingReader = new SecretAppsettingReader();
var connectionStrings = secretAppsettingReader.ReadSection<ConnectionStrings>("ConnectionStrings");

string connectionString = connectionStrings.DefaultConnectionString; 
string containerName = "examplecontainer";
string blobName = "exampleblob";
string filePath = @"C:\Temp\example.txt";
string destinationPath = @"C:\Temp\example2.txt";

UploadCompressedFileToBlobStorageAsync(connectionString, containerName, blobName, filePath).Wait();
DownloadAndDecompressFileFromBlobStorageAsync(connectionString, containerName, blobName, destinationPath).Wait();

async Task UploadCompressedFileToBlobStorageAsync(string connectionString, string containerName, string blobName, string filePath)
{
    // Create a BlobServiceClient to interact with the Blob Storage
    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

    // Get a reference to the container where you want to store the compressed data
    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

    // Compress the data before uploading
    using (MemoryStream compressedStream = new MemoryStream())
    {
        using (FileStream fileStream = File.OpenRead(filePath))
        {
            using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, true))
            {
                fileStream.Position = 0;
                await fileStream.CopyToAsync(gzipStream);
            }
            // Upload the compressed data to Blob Storage
            compressedStream.Position = 0;
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(compressedStream, true);
        }
    }
}

async Task DownloadAndDecompressFileFromBlobStorageAsync(string connectionString, string containerName, string blobName, string destinationPath)
{
    // Create a BlobServiceClient to interact with the Blob Storage
    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

    // Get a reference to the container where the compressed data is stored
    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

    // Download the compressed data from Blob Storage
    BlobClient blobClient = containerClient.GetBlobClient(blobName);
    using MemoryStream compressedStream = new MemoryStream();
    await blobClient.DownloadToAsync(compressedStream);
    compressedStream.Position = 0;

    // Decompress the data before saving it to the destination
    using FileStream destinationStream = File.Create(destinationPath);
    using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, true))
    {
        await gzipStream.CopyToAsync(destinationStream);
    }
}


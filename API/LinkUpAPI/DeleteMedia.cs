using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using Azure.Storage.Blobs;
using System.IO;

public static class DeleteMediaFunction
{
    private static IMongoCollection<Media> _mediaCollection;

    static DeleteMediaFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _mediaCollection = database.GetCollection<Media>("media");
    }

    [FunctionName("DeleteMedia")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("DeleteMedia function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var mediaData = JsonConvert.DeserializeObject<MediaRequest>(requestBody);
        string mediaId = mediaData?.Id;

        if (string.IsNullOrEmpty(mediaId))
        {
            return new BadRequestObjectResult("Media ID is missing or incorrect.");
        }

        var mediaToDelete = await _mediaCollection.Find(m => m.Id == mediaId).FirstOrDefaultAsync();
        if (mediaToDelete != null)
        {
            if (!string.IsNullOrEmpty(mediaToDelete.Url))
            {
                var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var blobUriBuilder = new BlobUriBuilder(new Uri(mediaToDelete.Url));
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
                var blobClient = blobContainerClient.GetBlobClient(blobUriBuilder.BlobName);

                await blobClient.DeleteIfExistsAsync();
            }

            var deleteFilter = Builders<Media>.Filter.Eq(m => m.Id, mediaId);
            await _mediaCollection.DeleteOneAsync(deleteFilter);
        }
        else
        {
            return new NotFoundResult();
        }

        return new OkResult();
    }
}
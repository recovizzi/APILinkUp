using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.IO;
using System;
using Azure.Storage.Blobs;
using System.Linq;

public static class UpdateMediaFunction
{
    private static IMongoCollection<Media> _mediaCollection;

    static UpdateMediaFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _mediaCollection = database.GetCollection<Media>("media");
    }

    [FunctionName("UpdateMedia")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("UpdateMedia function processed a request.");

        var formData = await req.ReadFormAsync();
        var file = req.Form.Files["file"];
        string mediaId = req.Form["id"];

        if (string.IsNullOrEmpty(mediaId) || file == null || file.Length == 0)
        {
            return new BadRequestObjectResult("Media ID or file is missing or incorrect.");
        }

        var mediaToUpdate = await _mediaCollection.Find(m => m.Id == mediaId).FirstOrDefaultAsync();
        var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

        if (mediaToUpdate != null && !string.IsNullOrEmpty(mediaToUpdate.Url))
        {
            var blobUriBuilder = new BlobUriBuilder(new Uri(mediaToUpdate.Url));
            var oldBlobClient = blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName).GetBlobClient(blobUriBuilder.BlobName);
            await oldBlobClient.DeleteIfExistsAsync();
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var containerClient = blobServiceClient.GetBlobContainerClient("media");
        var newBlobClient = containerClient.GetBlobClient(fileName);

        using (var stream = file.OpenReadStream())
        {
            await newBlobClient.UploadAsync(stream, overwrite: true);
        }

        var mediaUrl = newBlobClient.Uri.ToString();

        var updateDefinition = Builders<Media>.Update
            .Set(m => m.Url, mediaUrl)
            .Set(m => m.Timestamp, DateTime.UtcNow);

        var filter = Builders<Media>.Filter.Eq(m => m.Id, mediaId);
        var updateResult = await _mediaCollection.UpdateOneAsync(filter, updateDefinition);

        if (updateResult.MatchedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(new { Id = mediaId, Url = mediaUrl });
    }
}
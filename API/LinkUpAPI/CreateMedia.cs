using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Text;
using Azure.Storage.Blobs;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public static class CreateMediaFunction
{
    private static IMongoCollection<Media> _mediaCollection;
    private static IMongoCollection<User> _usersCollection;

    static CreateMediaFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _mediaCollection = database.GetCollection<Media>("media");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("CreateMedia")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("CreateMedia function processed a request.");

        // Token validation
        string authHeader = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return new UnauthorizedResult();
        }

        string token = authHeader.Substring("Bearer ".Length).Trim();
        var userId = ValidateTokenAndGetUserId(token);
        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        // Handle media file upload
        var formdata = await req.ReadFormAsync();
        var file = req.Form.Files["file"];
        if (file == null || file.Length == 0)
        {
            return new BadRequestObjectResult("No file was uploaded.");
        }

        // Generate a unique file name for storage
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        // Vérifier si le conteneur existe, sinon le créer
        var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("media");
        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "media", fileName);

        // Upload the file
        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, true);
        }

        var mediaUrl = blobClient.Uri.ToString();
        var media = new Media
        {
            UserId = userId,
            Type = file.ContentType.StartsWith("image/") ? "image" : "video",
            Url = mediaUrl,
            Timestamp = DateTime.UtcNow
        };

        await _mediaCollection.InsertOneAsync(media);

        return new OkObjectResult(media);
    }

    private static string ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(token);
            var decodedUsername = Encoding.UTF8.GetString(decodedBytes);
            var user = _usersCollection.Find(u => u.Username == decodedUsername).FirstOrDefault();
            return user?.Id;
        }
        catch
        {
            return null;
        }
    }
}

public class Media
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Type { get; set; }
    public string Url { get; set; }
    public DateTime Timestamp { get; set; }
}

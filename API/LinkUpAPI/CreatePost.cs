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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public static class CreatePostFunction
{
    private static IMongoCollection<Post> _postsCollection;
    private static IMongoCollection<User> _usersCollection;

    static CreatePostFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _postsCollection = database.GetCollection<Post>("posts");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("CreatePost")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("CreatePost function processed a request.");

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

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var post = JsonConvert.DeserializeObject<Post>(requestBody);
        post.UserId = userId;
        post.Timestamp = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(post.MediaId))
        {
            // Validate or process the media ID if necessary
        }

        await _postsCollection.InsertOneAsync(post);

        return new OkObjectResult(post);
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

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public string MediaId { get; set; }
}

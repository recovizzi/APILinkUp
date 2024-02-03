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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text;

public static class CreateCommentFunction
{
    private static IMongoCollection<Comment> _commentsCollection;
    private static IMongoCollection<User> _usersCollection;

    static CreateCommentFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _commentsCollection = database.GetCollection<Comment>("comments");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("CreateComment")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        string authHeader = req.Headers["Authorization"];
        string token = authHeader?.Substring("Bearer ".Length).Trim();
        var userId = ValidateTokenAndGetUserId(token);

        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var comment = JsonConvert.DeserializeObject<Comment>(requestBody);
        comment.UserId = userId;
        comment.Timestamp = DateTime.UtcNow;

        await _commentsCollection.InsertOneAsync(comment);

        return new OkObjectResult(comment);
    }

    private static string ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(token);
            var decodedUsername = Encoding.UTF8.GetString(decodedBytes);

            var filter = Builders<User>.Filter.Eq(u => u.Username, decodedUsername);
            var user = _usersCollection.Find(filter).FirstOrDefault();

            return user?.Id;
        }
        catch
        {
            return null;
        }
    }

}

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string UserId { get; set; }
    public string PostId { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

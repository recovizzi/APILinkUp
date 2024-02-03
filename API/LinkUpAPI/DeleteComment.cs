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

public static class DeleteCommentFunction
{
    private static IMongoCollection<Comment> _commentsCollection;
    private static IMongoCollection<User> _usersCollection;

    static DeleteCommentFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _commentsCollection = database.GetCollection<Comment>("comments");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("DeleteComment")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("DeleteComment function processed a request.");

        string authHeader = req.Headers["Authorization"];
        string token = authHeader?.Substring("Bearer ".Length).Trim();
        var userId = ValidateTokenAndGetUserId(token);

        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestComment = JsonConvert.DeserializeObject<CommentRequest>(requestBody);

        if (requestComment == null || string.IsNullOrEmpty(requestComment.Id))
        {
            return new BadRequestObjectResult("Comment ID is missing or incorrect.");
        }

        var filter = Builders<Comment>.Filter.Eq(c => c.Id, requestComment.Id) & Builders<Comment>.Filter.Eq(c => c.UserId, userId);
        var result = await _commentsCollection.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkResult();
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

public class CommentRequest
{
    public string Id { get; set; }
}
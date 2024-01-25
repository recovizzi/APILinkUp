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

public static class UpdateCommentFunction
{
    private static IMongoCollection<Comment> _commentsCollection;
    private static IMongoCollection<User> _usersCollection;

    static UpdateCommentFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _commentsCollection = database.GetCollection<Comment>("comments");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("UpdateComment")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("UpdateComment function processed a request.");

        string authHeader = req.Headers["Authorization"];
        string token = authHeader?.Substring("Bearer ".Length).Trim();
        var userId = ValidateTokenAndGetUserId(token);

        if (string.IsNullOrEmpty(userId))
        {
            return new UnauthorizedResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedComment = JsonConvert.DeserializeObject<Comment>(requestBody);

        if (updatedComment == null || string.IsNullOrEmpty(updatedComment.Id))
        {
            return new BadRequestObjectResult("Comment ID is missing or incorrect.");
        }

        var filter = Builders<Comment>.Filter.Eq(c => c.Id, updatedComment.Id) & Builders<Comment>.Filter.Eq(c => c.UserId, userId);
        var update = Builders<Comment>.Update.Set(c => c.Content, updatedComment.Content).Set(c => c.Timestamp, DateTime.UtcNow);

        var result = await _commentsCollection.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(updatedComment);
    }

    private static string ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(token);
            var decodedUsername = Encoding.UTF8.GetString(decodedBytes);

            var filter = Builders<User>.Filter.Eq(u => u.Username, decodedUsername);
            var user = _usersCollection.Find(filter).FirstOrDefault();

            return user?.Id; // Retourne l'ID de l'utilisateur si trouvé, sinon null
        }
        catch
        {
            return null; // Retourne null en cas d'erreur
        }
    }
}
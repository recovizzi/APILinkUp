using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

public static class UpdatePostFunction
{

    private static IMongoCollection<Post> _postsCollection;
    private static IMongoCollection<User> _usersCollection;

    static UpdatePostFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _postsCollection = database.GetCollection<Post>("posts");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("UpdatePost")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("UpdatePost function processed a request.");

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
        var updatedPost = JsonConvert.DeserializeObject<Post>(requestBody);

        if (updatedPost == null || string.IsNullOrEmpty(updatedPost.Id))
        {
            return new BadRequestObjectResult("Post ID is missing or incorrect.");
        }

        var filter = Builders<Post>.Filter.Eq(p => p.Id, updatedPost.Id) & Builders<Post>.Filter.Eq(p => p.UserId, userId);
        var update = Builders<Post>.Update;

        var updateDefinitions = new List<UpdateDefinition<Post>>();

        if (!string.IsNullOrEmpty(updatedPost.Content))
        {
            updateDefinitions.Add(update.Set(p => p.Content, updatedPost.Content));
        }

        if (!string.IsNullOrEmpty(updatedPost.MediaId))
        {
            updateDefinitions.Add(update.Set(p => p.MediaId, updatedPost.MediaId));
        }

        // Add other fields to update as needed

        if (updateDefinitions.Count == 0)
        {
            return new BadRequestObjectResult("No fields to update were provided.");
        }

        var combinedUpdate = update.Combine(updateDefinitions);
        var result = await _postsCollection.UpdateOneAsync(filter, combinedUpdate);

        if (result.MatchedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(updatedPost);
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
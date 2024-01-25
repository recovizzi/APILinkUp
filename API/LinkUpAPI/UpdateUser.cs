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

public static class UpdateUserFunction
{
    private static IMongoCollection<User> _usersCollection;

    static UpdateUserFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("UpdateUser")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("UpdateUser function processed a request.");

        // Token validation
        string authHeader = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return new UnauthorizedResult();
        }

        string token = authHeader.Substring("Bearer ".Length).Trim();
        if (!ValidateToken(token))
        {
            return new UnauthorizedResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedUser = JsonConvert.DeserializeObject<User>(requestBody);

        if (updatedUser == null || string.IsNullOrEmpty(updatedUser.Id))
        {
            return new BadRequestObjectResult("User ID is missing or incorrect.");
        }

        var filter = Builders<User>.Filter.Eq(u => u.Id, updatedUser.Id);
        var update = Builders<User>.Update;

        var updateDefinitions = new List<UpdateDefinition<User>>();

        if (!string.IsNullOrEmpty(updatedUser.Username))
        {
            updateDefinitions.Add(update.Set(u => u.Username, updatedUser.Username));
        }

        if (!string.IsNullOrEmpty(updatedUser.Email))
        {
            updateDefinitions.Add(update.Set(u => u.Email, updatedUser.Email));
        }

        // Add other fields to update as needed

        if (updateDefinitions.Count == 0)
        {
            return new BadRequestObjectResult("No fields to update were provided.");
        }

        var combinedUpdate = update.Combine(updateDefinitions);
        var result = await _usersCollection.UpdateOneAsync(filter, combinedUpdate);

        if (result.MatchedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(updatedUser);
    }

    private static bool ValidateToken(string token)
    {
        try
        {
            var decodedBytes = Convert.FromBase64String(token);
            var decodedUsername = Encoding.UTF8.GetString(decodedBytes);
            var filter = Builders<User>.Filter.Eq(u => u.Username, decodedUsername);
            var user = _usersCollection.Find(filter).FirstOrDefault();
            return user != null;
        }
        catch
        {
            return false;
        }
    }
}
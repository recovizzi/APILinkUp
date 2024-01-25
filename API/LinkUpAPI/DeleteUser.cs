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

public static class DeleteUserFunction
{
    private static IMongoCollection<User> _usersCollection;

    static DeleteUserFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("DeleteUser")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("DeleteUser function processed a request.");

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
        var requestUser = JsonConvert.DeserializeObject<User>(requestBody);

        if (requestUser == null || string.IsNullOrEmpty(requestUser.Id))
        {
            return new BadRequestObjectResult("User ID is missing or incorrect.");
        }

        var filter = Builders<User>.Filter.Eq(u => u.Id, requestUser.Id);
        var result = await _usersCollection.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkResult();
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
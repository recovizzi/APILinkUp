using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System;

public static class SearchUsersFunction
{
    private static IMongoCollection<User> _usersCollection;

    static SearchUsersFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("SearchUsers")]
    public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(requestBody);
        var searchTerm = searchRequest.SearchTerm;

        var filter = Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
        var users = await _usersCollection.Find(filter).ToListAsync();

        var userResults = users.Select(u => new { u.Id, u.Username }).ToList();

        return new OkObjectResult(userResults);
    }
}

public class SearchRequest
{
    public string SearchTerm { get; set; }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Text;
using Newtonsoft.Json;
using System.IO;

public static class GetPostFunction
{
    private static IMongoCollection<Post> _postsCollection;
    private static IMongoCollection<User> _usersCollection;

    static GetPostFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _postsCollection = database.GetCollection<Post>("posts");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("GetPost")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("GetPost function processed a request.");

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
var requestPost = JsonConvert.DeserializeObject<PostRequestData>(requestBody);


    if (requestPost == null || string.IsNullOrEmpty(requestPost.Id))
    {
        return new BadRequestObjectResult("Post ID is missing or incorrect.");
    }

    var filter = Builders<Post>.Filter.Eq(p => p.Id, requestPost.Id);
    var post = await _postsCollection.Find(filter).FirstOrDefaultAsync();

    if (post == null)
    {
        return new NotFoundResult();
    }

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

public class PostRequestData
{
    public string Id { get; set; }
}

}
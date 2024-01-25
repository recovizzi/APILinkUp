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
using MongoDB.Bson;
using System;

public static class GetLastPostsFunction
{
    private static IMongoCollection<Post> _postsCollection;
    private static IMongoCollection<User> _usersCollection;

    static GetLastPostsFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _postsCollection = database.GetCollection<Post>("posts");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("GetLastPosts")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("GetLastPosts function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<PostsRequest>(requestBody);
        
        if (request?.NumberOfPosts <= 0)
        {
            return new BadRequestObjectResult("Invalid number of posts requested.");
        }

                FilterDefinition<Post> filter = Builders<Post>.Filter.Empty;
        if (!string.IsNullOrEmpty(request.StartAfterId))
        {
            filter = Builders<Post>.Filter.Lt(p => p.Id, request.StartAfterId);
        }

        var posts = await _postsCollection.Find(filter)
                                         .Sort(Builders<Post>.Sort.Descending(p => p.Timestamp))
                                         .Limit(request.NumberOfPosts)
                                         .ToListAsync();

        var postUserInfos = await Task.WhenAll(posts.Select(async p =>
        {
            var user = await _usersCollection.Find(u => u.Id == p.UserId).FirstOrDefaultAsync();
            return new
            {
                PostId = p.Id.ToString(),
                Username = user?.Username,
                p.Content,
                p.MediaId,
                p.Timestamp
            };
        }));

        return new OkObjectResult(postUserInfos);
    }
}

public class PostsRequest
{
    public int NumberOfPosts { get; set; }
    public string StartAfterId { get; set; }
}
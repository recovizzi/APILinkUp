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

public static class GetCommentFunction
{
    private static IMongoCollection<Comment> _commentsCollection;

    static GetCommentFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _commentsCollection = database.GetCollection<Comment>("comments");
    }

    [FunctionName("GetComment")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("GetComment function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestPost = JsonConvert.DeserializeObject<PostRequestData>(requestBody);
if (requestPost == null || string.IsNullOrEmpty(requestPost.Id))
{
return new BadRequestObjectResult("Post ID is missing or incorrect.");
}

    var filter = Builders<Comment>.Filter.Eq(c => c.PostId, requestPost.Id);
    var comments = await _commentsCollection.Find(filter).ToListAsync();

    return new OkObjectResult(comments);
}
}

public class PostRequestData
{
public string Id { get; set; }
}
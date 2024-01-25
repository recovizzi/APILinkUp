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

public static class DeleteAllCommentsFunction
{
    private static IMongoCollection<Comment> _commentsCollection;

    static DeleteAllCommentsFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _commentsCollection = database.GetCollection<Comment>("comments");
    }

    [FunctionName("DeleteAllComments")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("DeleteAllComments function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestPost = JsonConvert.DeserializeObject<PostRequestData>(requestBody);

        if (requestPost == null || string.IsNullOrEmpty(requestPost.Id))
        {
            return new BadRequestObjectResult("Post ID is missing or incorrect.");
        }

        var filter = Builders<Comment>.Filter.Eq(c => c.PostId, requestPost.Id);
        var result = await _commentsCollection.DeleteManyAsync(filter);

        if (result.DeletedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkResult();
    }
}

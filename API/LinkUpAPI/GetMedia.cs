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

public static class GetMediaFunction
{
    private static IMongoCollection<Media> _mediaCollection;

    static GetMediaFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _mediaCollection = database.GetCollection<Media>("media");
    }

    [FunctionName("GetMedia")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("GetMedia function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestData = JsonConvert.DeserializeObject<MediaRequest>(requestBody);
        if (requestData == null || string.IsNullOrEmpty(requestData.Id))
        {
            return new BadRequestObjectResult("Media ID is missing or incorrect.");
        }

        var media = await _mediaCollection.Find(m => m.Id == requestData.Id).FirstOrDefaultAsync();
        if (media == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(media);
    }
}

public class MediaRequest
{
    public string Id { get; set; }
}


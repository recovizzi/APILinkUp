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
using System.Net.Http;

public static class DeletePostFunction
{
    private static IMongoCollection<Post> _postsCollection;
    private static IMongoCollection<User> _usersCollection;

    static DeletePostFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _postsCollection = database.GetCollection<Post>("posts");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("DeletePost")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("DeletePost function processed a request.");

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
        var requestPost = JsonConvert.DeserializeObject<Post>(requestBody);

        if (requestPost == null || string.IsNullOrEmpty(requestPost.Id))
        {
            return new BadRequestObjectResult("Post ID is missing or incorrect.");
        }

        var filter = Builders<Post>.Filter.Eq(p => p.Id, requestPost.Id) & Builders<Post>.Filter.Eq(p => p.UserId, userId);
        var result = await _postsCollection.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
        {
            return new NotFoundResult();
        }


        // if (result.DeletedCount > 0 && !string.IsNullOrEmpty(requestPost.MediaId))
        // {
        //     using (var httpClient = new HttpClient())
        //     {
        //         httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //         var mediaRequest = new { Id = requestPost.MediaId };
        //         var mediaRequestJson = JsonConvert.SerializeObject(mediaRequest);
        //         var request = new HttpRequestMessage
        //         {
        //             Method = HttpMethod.Delete,
        //             RequestUri = new Uri(Environment.GetEnvironmentVariable("DeleteMediaURL")),
        //             Content = new StringContent(mediaRequestJson, Encoding.UTF8, "application/json")
        //         };

        //         await httpClient.SendAsync(request);
        //     }
        // }

        return new OkResult();
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
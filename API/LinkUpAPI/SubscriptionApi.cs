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
using System.Linq;

public static class SubscriptionApi
{
    private static IMongoCollection<Subscription> _subscriptionsCollection;

    static SubscriptionApi()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _subscriptionsCollection = database.GetCollection<Subscription>("subscriptions");
    }

    [FunctionName("Subscribe")]
    public static async Task<IActionResult> Subscribe(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var subscription = JsonConvert.DeserializeObject<Subscription>(requestBody);

        await _subscriptionsCollection.InsertOneAsync(subscription);

        return new OkObjectResult("Subscription created successfully.");
    }

    [FunctionName("Unsubscribe")]
    public static async Task<IActionResult> Unsubscribe(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
    ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var subscription = JsonConvert.DeserializeObject<Subscription>(requestBody);

        var filter = Builders<Subscription>.Filter.Eq(s => s.UserId, subscription.UserId) &
                     Builders<Subscription>.Filter.Eq(s => s.FollowedUserId, subscription.FollowedUserId);

        var result = await _subscriptionsCollection.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
        {
            return new NotFoundResult();
        }

        return new OkResult();
    }

    [FunctionName("Subscriptions")]
    public static async Task<IActionResult> Subscriptions(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var userRequest = JsonConvert.DeserializeObject<UserRequest>(requestBody);

        if (userRequest == null || string.IsNullOrEmpty(userRequest.UserId))
        {
            return new BadRequestObjectResult("User ID is required.");
        }

        var filter = Builders<Subscription>.Filter.Eq(s => s.UserId, userRequest.UserId);
        var subscriptions = await _subscriptionsCollection.Find(filter).ToListAsync();

        var followedUserIds = subscriptions.Select(s => s.FollowedUserId).ToList();

        return new OkObjectResult(followedUserIds);
    }

    public class UserRequest
    {
        public string UserId { get; set; }
    }

}
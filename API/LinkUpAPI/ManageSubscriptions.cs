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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public static class ManageSubscriptions
{
    private static IMongoCollection<Subscription> _subscriptionsCollection;

    static ManageSubscriptions()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _subscriptionsCollection = database.GetCollection<Subscription>("subscriptions");
    }

    [FunctionName("AddSubscription")]
    public static async Task<IActionResult> Add(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var subscription = JsonConvert.DeserializeObject<Subscription>(requestBody);
        
        await _subscriptionsCollection.InsertOneAsync(subscription);

        return new OkResult();
    }

    [FunctionName("RemoveSubscription")]
    public static async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
        ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var subscription = JsonConvert.DeserializeObject<Subscription>(requestBody);
        
        var filter = Builders<Subscription>.Filter.Eq(s => s.UserId, subscription.UserId) & Builders<Subscription>.Filter.Eq(s => s.FollowedUserId, subscription.FollowedUserId);
        await _subscriptionsCollection.DeleteOneAsync(filter);

        return new OkResult();
    }
}

public class Subscription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("UserId")]
    public string UserId { get; set; }

    [BsonElement("FollowedUserId")]
    public string FollowedUserId { get; set; }
}


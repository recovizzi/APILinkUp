using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Linq;
using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public static class GetNotificationsFunction
{
    private static IMongoCollection<Notification> _notificationsCollection;

    static GetNotificationsFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _notificationsCollection = database.GetCollection<Notification>("notifications");
    }

    [FunctionName("GetNotifications")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        string userId = req.Query["userId"];

        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId) & Builders<Notification>.Filter.Eq(n => n.IsRead, false);
        var notifications = await _notificationsCollection.Find(filter).ToListAsync();

        return new OkObjectResult(notifications);
    }
}

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime Timestamp { get; set; }
}

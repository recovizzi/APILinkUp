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

public static class UpdateNotificationsFunction
{
    private static IMongoCollection<Notification> _notificationsCollection;

    static UpdateNotificationsFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _notificationsCollection = database.GetCollection<Notification>("notifications");
    }

    [FunctionName("UpdateNotifications")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
        ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updateRequest = JsonConvert.DeserializeObject<UpdateNotificationRequest>(requestBody);

        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, updateRequest.UserId);
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        await _notificationsCollection.UpdateManyAsync(filter, update);

        return new OkResult();
    }
}

public class UpdateNotificationRequest
{
    public string UserId { get; set; }
}


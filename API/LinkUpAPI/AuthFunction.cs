using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Cryptography;

public static class AuthFunction
{
    private static IMongoCollection<User> _usersCollection;

    static AuthFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("AuthFunction")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request for authentication.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JObject.Parse(requestBody);
        string username = data?.username;
        string password = data?.password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return new BadRequestResult();
        }

        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

        if (user != null)
        {
            var hashedPassword = HashPassword(password);
            if (user.PasswordHash == hashedPassword)
            {
                // Generate a token
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
                return new OkObjectResult(new { token = token });
            }
        }

        return new BadRequestResult();
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
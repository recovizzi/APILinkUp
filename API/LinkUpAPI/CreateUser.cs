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
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public static class CreateUserFunction
{
    private static IMongoCollection<User> _usersCollection;

    static CreateUserFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("CreateUser")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("CreateUser function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var user = JsonConvert.DeserializeObject<User>(requestBody);

        if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.PasswordHash))
        {
            return new BadRequestObjectResult("User data is missing or incorrect.");
        }

        var existingUser = await _usersCollection.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            return new BadRequestObjectResult("Email is already in use.");
        }

        // Hash the password before saving the user to the database
        user.PasswordHash = CreateUserFunction.HashPassword(user.PasswordHash);

        await _usersCollection.InsertOneAsync(user);
        return new CreatedResult("User", user);
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

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; } // Storing the hash of the password, not the password itself
    public string Email { get; set; }
    // Add other properties as needed
}

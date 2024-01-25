using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Text;
using System.IO;
using Newtonsoft.Json;

public static class GetUserFunction
{
    private static IMongoCollection<User> _usersCollection;

    static GetUserFunction()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnection"));
        var database = client.GetDatabase("SocialMediaDB");
        _usersCollection = database.GetCollection<User>("users");
    }

    [FunctionName("GetUser")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("GetUser function processed a request.");

        // Token validation
        string authHeader = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return new UnauthorizedResult();
        }

        string token = authHeader.Substring("Bearer ".Length).Trim();
        if (!ValidateToken(token))
        {
            return new UnauthorizedResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestUser = JsonConvert.DeserializeObject<User>(requestBody);

        if (requestUser == null || string.IsNullOrEmpty(requestUser.Id))
        {
            return new BadRequestObjectResult("User ID is missing or incorrect.");
        }

        var filter = Builders<User>.Filter.Eq(u => u.Id, requestUser.Id);
        var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

        if (user == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(user);
    }

    private static bool ValidateToken(string token)
    {
        try
        {
            // Décoder le token de Base64
            var decodedBytes = Convert.FromBase64String(token);
            var decodedUsername = Encoding.UTF8.GetString(decodedBytes);

            // Vérifier si un utilisateur avec ce username existe dans la base de données
            var filter = Builders<User>.Filter.Eq(u => u.Username, decodedUsername);
            var user = _usersCollection.Find(filter).FirstOrDefault();

            // Si un utilisateur existe avec ce username, le token est considéré comme valide
            return user != null;
        }
        catch
        {
            // En cas d'erreur lors du décodage ou de la recherche, le token est invalide
            return false;
        }
    }
}
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Cosmonauta.Models;

namespace Cosmonauta.Services;

public class CurlGeneratorService
{
    public CurlExample GenerateListDatabasesCurl(string endpoint, string masterKey)
    {
        var uri = new Uri($"{endpoint.TrimEnd('/')}/dbs");
        var dateTime = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        var authHeader = GenerateAuthorizationHeader("GET", "dbs", "", masterKey, dateTime);

        return new CurlExample
        {
            Method = "GET",
            Url = uri.ToString(),
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["Accept"] = "application/json"
            },
            CurlCommand = GenerateCurlCommand("GET", uri.ToString(), new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["Accept"] = "application/json"
            }, null)
        };
    }

    public CurlExample GenerateListCollectionsCurl(string endpoint, string databaseId, string masterKey)
    {
        var uri = new Uri($"{endpoint.TrimEnd('/')}/dbs/{databaseId}/colls");
        var dateTime = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        var authHeader = GenerateAuthorizationHeader("GET", "colls", $"dbs/{databaseId}", masterKey, dateTime);

        return new CurlExample
        {
            Method = "GET",
            Url = uri.ToString(),
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["Accept"] = "application/json"
            },
            CurlCommand = GenerateCurlCommand("GET", uri.ToString(), new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["Accept"] = "application/json"
            }, null)
        };
    }

    public CurlExample GenerateQueryDocumentsCurl(string endpoint, string databaseId, string collectionId, string masterKey, string query)
    {
        var uri = new Uri($"{endpoint.TrimEnd('/')}/dbs/{databaseId}/colls/{collectionId}/docs");
        var dateTime = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        var authHeader = GenerateAuthorizationHeader("POST", "docs", $"dbs/{databaseId}/colls/{collectionId}", masterKey, dateTime);

        var body = $@"{{
    ""query"": ""{query.Replace("\"", "\\\"")}"",
    ""parameters"": []
}}";

        return new CurlExample
        {
            Method = "POST",
            Url = uri.ToString(),
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["x-ms-documentdb-isquery"] = "True",
                ["Content-Type"] = "application/query+json",
                ["Accept"] = "application/json",
                ["x-ms-documentdb-query-enablecrosspartition"] = "true"
            },
            Body = body,
            CurlCommand = GenerateCurlCommand("POST", uri.ToString(), new Dictionary<string, string>
            {
                ["Authorization"] = authHeader,
                ["x-ms-date"] = dateTime,
                ["x-ms-version"] = "2018-12-31",
                ["x-ms-documentdb-isquery"] = "True",
                ["Content-Type"] = "application/query+json",
                ["Accept"] = "application/json",
                ["x-ms-documentdb-query-enablecrosspartition"] = "true"
            }, body)
        };
    }

    private string GenerateAuthorizationHeader(string verb, string resourceType, string resourceLink, string masterKey, string dateTime)
    {
        var keyBytes = Convert.FromBase64String(masterKey);
        var text = $"{verb.ToLowerInvariant()}\n{resourceType.ToLowerInvariant()}\n{resourceLink}\n{dateTime.ToLowerInvariant()}\n\n";
        
        using (var hmacSha256 = new HMACSHA256(keyBytes))
        {
            var hashPayload = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            var signature = Convert.ToBase64String(hashPayload);
            var authString = $"type=master&ver=1.0&sig={signature}";
            return HttpUtility.UrlEncode(authString);
        }
    }

    private string GenerateCurlCommand(string method, string url, Dictionary<string, string> headers, string? body)
    {
        var sb = new StringBuilder();
        sb.Append($"curl -X {method} \\\n");
        sb.Append($"  '{url}' \\\n");
        
        foreach (var header in headers)
        {
            sb.Append($"  -H '{header.Key}: {header.Value}' \\\n");
        }
        
        if (!string.IsNullOrEmpty(body))
        {
            var escapedBody = body.Replace("'", "'\\''").Replace("\n", "\\n").Replace("\r", "\\r");
            sb.Append($"  -d '{escapedBody}'");
        }
        else
        {
            sb.Remove(sb.Length - 3, 3);
        }
        
        return sb.ToString();
    }
}
using Newtonsoft.Json;

namespace Cosmonauta.Models;

public class ResourceGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class CosmosAccountInfo
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DocumentEndpoint { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string PrimaryMasterKey { get; set; } = string.Empty;
}

public class DatabaseInfo
{
    public string Id { get; set; } = string.Empty;
    public string SelfLink { get; set; } = string.Empty;
}

public class CollectionInfo
{
    public string Id { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public string SelfLink { get; set; } = string.Empty;
}

public class DocumentInfo
{
    public string Id { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    [JsonExtensionData]
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class QueryRequest
{
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public int MaxItemCount { get; set; } = 100;
}

public class CurlExample
{
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string CurlCommand { get; set; } = string.Empty;
}
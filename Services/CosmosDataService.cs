using Microsoft.Azure.Cosmos;
using Cosmonauta.Models;
using Newtonsoft.Json;
using System.Net;

namespace Cosmonauta.Services;

public class CosmosDataService : IDisposable
{
    private CosmosClient? _cosmosClient;
    private string _currentEndpoint = string.Empty;
    private string _currentKey = string.Empty;

    public void Initialize(string endpoint, string key)
    {
        _currentEndpoint = endpoint;
        _currentKey = key;
        
        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        _cosmosClient?.Dispose();
        _cosmosClient = new CosmosClient(endpoint, key, clientOptions);
    }

    public async Task<List<DatabaseInfo>> GetDatabasesAsync()
    {
        if (_cosmosClient == null)
            throw new InvalidOperationException("CosmosClient not initialized. Call Initialize first.");

        var databases = new List<DatabaseInfo>();
        
        using var iterator = _cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var database in response)
            {
                databases.Add(new DatabaseInfo
                {
                    Id = database.Id,
                    SelfLink = database.SelfLink
                });
            }
        }

        return databases;
    }

    public async Task<List<CollectionInfo>> GetCollectionsAsync(string databaseId)
    {
        if (_cosmosClient == null)
            throw new InvalidOperationException("CosmosClient not initialized. Call Initialize first.");

        var collections = new List<CollectionInfo>();
        var database = _cosmosClient.GetDatabase(databaseId);
        
        using var iterator = database.GetContainerQueryIterator<ContainerProperties>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var container in response)
            {
                var partitionKeyPath = container.PartitionKeyPath ?? "/id";
                collections.Add(new CollectionInfo
                {
                    Id = container.Id,
                    PartitionKey = partitionKeyPath,
                    SelfLink = container.SelfLink
                });
            }
        }

        return collections;
    }

    public async Task<List<DocumentInfo>> GetDocumentsAsync(string databaseId, string collectionId, int maxItems = 100)
    {
        if (_cosmosClient == null)
            throw new InvalidOperationException("CosmosClient not initialized. Call Initialize first.");

        var documents = new List<DocumentInfo>();
        var container = _cosmosClient.GetContainer(databaseId, collectionId);
        
        var queryOptions = new QueryRequestOptions
        {
            MaxItemCount = maxItems
        };

        using var iterator = container.GetItemQueryIterator<dynamic>(
            queryText: "SELECT * FROM c",
            requestOptions: queryOptions);

        while (iterator.HasMoreResults && documents.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                var json = JsonConvert.SerializeObject(item);
                var doc = JsonConvert.DeserializeObject<DocumentInfo>(json);
                if (doc != null)
                {
                    documents.Add(doc);
                }
            }
        }

        return documents;
    }

    public async Task<List<DocumentInfo>> ExecuteQueryAsync(string databaseId, string collectionId, QueryRequest queryRequest)
    {
        if (_cosmosClient == null)
            throw new InvalidOperationException("CosmosClient not initialized. Call Initialize first.");

        var documents = new List<DocumentInfo>();
        var container = _cosmosClient.GetContainer(databaseId, collectionId);
        
        var queryOptions = new QueryRequestOptions
        {
            MaxItemCount = queryRequest.MaxItemCount
        };

        QueryDefinition queryDefinition;
        if (queryRequest.Parameters != null && queryRequest.Parameters.Any())
        {
            queryDefinition = new QueryDefinition(queryRequest.Query);
            foreach (var param in queryRequest.Parameters)
            {
                queryDefinition.WithParameter($"@{param.Key}", param.Value);
            }
        }
        else
        {
            queryDefinition = new QueryDefinition(queryRequest.Query);
        }

        using var iterator = container.GetItemQueryIterator<dynamic>(
            queryDefinition: queryDefinition,
            requestOptions: queryOptions);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                var json = JsonConvert.SerializeObject(item);
                var doc = JsonConvert.DeserializeObject<DocumentInfo>(json);
                if (doc != null)
                {
                    documents.Add(doc);
                }
            }
        }

        return documents;
    }

    public string GetCurrentEndpoint() => _currentEndpoint;
    public string GetCurrentKey() => _currentKey;

    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }
}
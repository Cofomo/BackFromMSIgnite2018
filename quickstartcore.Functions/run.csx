// this is the code as it runs on the Azure Portal.

#r "Microsoft.Azure.EventGrid"
#r "Microsoft.Azure.DocumentDB.Core"
#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq.Expressions;

public static async Task Run(EventGridEvent eventGridEvent, ILogger log)
{
    log.LogInformation("TodoFunction function started processing a request.");

    log.LogInformation(eventGridEvent.Data.ToString());

    var item = JsonConvert.DeserializeObject<Item>(eventGridEvent.Data.ToString());

    var cosmosDBRepository = new CosmosDBRepository<Item>();
    cosmosDBRepository.Initialize();
    await cosmosDBRepository.CreateItemAsync(item);
}

public class Item
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "isComplete")]
    public bool Completed { get; set; }
}


public class CosmosDBRepository<T> where T : class
{
    //Cosmos DB Core (SQL)
    private readonly string Endpoint = "https://learnandre.documents.azure.com:443/";
    private readonly string Key = "ziHqiRCSyR6PgNl7KHfSkUSaWuzeA98NZy3YXJ2rjclGtls9Dgdef8sAY4FryY2pgVnAzTVmKsiz9UTXfJHFxQ==";

    private readonly string DatabaseId = "ToDoList";
    private readonly string CollectionId = "Items";
    private DocumentClient client;

    public async Task<T> GetItemAsync(string id)
    {
        try
        {
            Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
            return (T)(dynamic)document;
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
    {
        IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
            UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
            new FeedOptions { MaxItemCount = -1 })
            .Where(predicate)
            .AsDocumentQuery();

        List<T> results = new List<T>();
        while (query.HasMoreResults)
        {
            results.AddRange(await query.ExecuteNextAsync<T>());
        }

        return results;
    }

    public async Task<Document> CreateItemAsync(T item)
    {
        return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
    }

    public async Task<Document> UpdateItemAsync(string id, T item)
    {
        return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
    }

    public async Task DeleteItemAsync(string id)
    {
        await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
    }

    public void Initialize()
    {
        client = new DocumentClient(new Uri(Endpoint), Key);
        CreateDatabaseIfNotExistsAsync().Wait();
        CreateCollectionIfNotExistsAsync().Wait();
    }

    private async Task CreateDatabaseIfNotExistsAsync()
    {
        try
        {
            await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
            }
            else
            {
                throw;
            }
        }
    }

    private async Task CreateCollectionIfNotExistsAsync()
    {
        try
        {
            await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await client.CreateDocumentCollectionAsync(
                    UriFactory.CreateDatabaseUri(DatabaseId),
                    new DocumentCollection { Id = CollectionId },
                    new RequestOptions { OfferThroughput = 1000 });
            }
            else
            {
                throw;
            }
        }
    }
}
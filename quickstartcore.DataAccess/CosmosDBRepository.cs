namespace todo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
  

    public class CosmosDBRepository<T> : ICosmosDBRepository<T> where T : class
    {

        // POur l'émulateur
        //private readonly string Endpoint = "https://localhost:8081";
        //private readonly string Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        //Cosmos DB Core (SQL)
        //private readonly string Endpoint = "https://learnandre.documents.azure.com:443/";
        //private readonly string Key = "ziHqiRCSyR6PgNl7KHfSkUSaWuzeA98NZy3YXJ2rjclGtls9Dgdef8sAY4FryY2pgVnAzTVmKsiz9UTXfJHFxQ==";

        //Cosmos DB Table API
        //private readonly string Endpoint = "https://ignite01table.table.cosmosdb.azure.com:443/";
        //private readonly string Key = "qANAObFjjBxiwiv1wHzpgxDnKc4vdwtJ6HYpfR2h736Ry7iJafYK4XobcYyV9bDb8vWRXZimJXDPoDB7kIwejw==";
        

        private readonly string DatabaseId = "ToDoList";
        private readonly string CollectionId = "Items";
        private DocumentClient client;

        private string _cosmosDbEndpoint;
        private string _cosmosDbKey;

        public CosmosDBRepository(string cosmosDbEndpoint, string cosmosDbKey)
        {
            _cosmosDbEndpoint = cosmosDbEndpoint;
            _cosmosDbKey = cosmosDbKey;
        }


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
            client = new DocumentClient(new Uri(_cosmosDbEndpoint), _cosmosDbKey);
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
}
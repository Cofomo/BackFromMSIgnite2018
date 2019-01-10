using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using todo;
using todo.Models;

namespace quickstartcore.Functions
{
	// This functions is triggered by an Azure EventGrid trigger. 
	// Thus, in order to test it locally, you need to get and setup ngrok on your development machine.
	// To learn more about ngrok and how to use it, please refer to the links in the "ngrok.txt" file.
    public static class TodoFunction
    {
        [FunctionName("TodoFunction")]
        public static async Task Run(
            EventGridEvent eventGridEvent, ICosmosDBRepository<Item> cosmosDBRepository,
            ILogger log)
        {
            log.LogInformation("TodoFunction function started processing a request.");

            log.LogInformation(eventGridEvent.Data.ToString());

            var item = JsonConvert.DeserializeObject<Item>(eventGridEvent.Data.ToString());

            cosmosDBRepository.Initialize();
            await cosmosDBRepository.CreateItemAsync(item);
        }
    }
}

namespace WeatherFunctionApp
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Blobs;
    using System.IO;
    using System;

    public static class GetPayload
    {
        [FunctionName("GetPayload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payload/{id}")] HttpRequest req,
            string id,
            ILogger log)
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            try
            {
                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("weatherpayloads");
                var blobClient = containerClient.GetBlobClient($"{id}.json");

                var response = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(response.Value.Content))
                {
                    var content = await reader.ReadToEndAsync();
                    return new OkObjectResult(content);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error fetching payload: {ex.Message}");
                return new NotFoundObjectResult("Payload not found");
            }
        }
    }
}

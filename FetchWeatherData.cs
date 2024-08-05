namespace WeatherFunctionApp
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Azure.Storage.Blobs;
    using WeatherFunctionApp.Entities;

    public static class FetchWeatherData
    {
        private static readonly HttpClient client = new HttpClient();
        private static string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static string weatherApiKey = Environment.GetEnvironmentVariable("WeatherApiKey");

        [FunctionName("FetchWeatherData")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string weatherUrl = $"https://api.openweathermap.org/data/2.5/weather?q=London&appid={weatherApiKey}";
            var timestamp = DateTime.Now;
            try
            {
                var response = await client.GetStringAsync(weatherUrl);

                // Store payload in Blob
                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("weatherpayloads");
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient($"{timestamp:yyyyMMddHHmmss}.json");
                await blobClient.UploadAsync(new BinaryData(response), true);

                await StoreLogInTable(timestamp, "Success", blobClient.Uri.ToString(), null);

                log.LogInformation($"Weather data fetched and stored successfully at {timestamp}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error fetching weather data: {ex.Message}");

                await StoreLogInTable(timestamp, "Failure", null, ex.Message);
            }
        }

        private static async Task StoreLogInTable(DateTime timestamp, string status, string blobUri = null, string errorMessage = null)
        {
            var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference("WeatherLogs");
            await table.CreateIfNotExistsAsync();

            var logEntry = new WeatherLogEntity
            {
                PartitionKey = "WeatherLog",
                RowKey = timestamp.ToString("yyyyMMddHHmmss"),
                Timestamp = timestamp,
                Status = status,
                BlobUri = blobUri,
                ErrorMessage = errorMessage
            };
            var insertOperation = TableOperation.Insert(logEntry);
            await table.ExecuteAsync(insertOperation);
        }
    }
}

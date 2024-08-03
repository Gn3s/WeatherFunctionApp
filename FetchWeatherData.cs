namespace WeatherFunctionApp
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Azure.Cosmos.Table;
    using Azure.Storage.Blobs;
    using System.Text.Json;
    using WeatherFunctionApp.Entities;
    using Google.Protobuf.WellKnownTypes;

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

                // Store log in Table Storage
                var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference("WeatherLogs");
                await table.CreateIfNotExistsAsync();

                var logEntry = new WeatherLogEntity
                {
                    PartitionKey = "WeatherLog",
                    RowKey = timestamp.ToString("yyyyMMddHHmmss"),
                    Timestamp = timestamp,
                    Status = "Success",
                    BlobUri = blobClient.Uri.ToString()
                };
                var insertOperation = TableOperation.Insert(logEntry);
                await table.ExecuteAsync(insertOperation);

                log.LogInformation($"Weather data fetched and stored successfully at {timestamp}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error fetching weather data: {ex.Message}");

                var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference("WeatherLogs");
                await table.CreateIfNotExistsAsync();

                var logEntry = new WeatherLogEntity
                {
                    PartitionKey = "WeatherLog",
                    RowKey = timestamp.ToString("yyyyMMddHHmmss"),
                    Timestamp = timestamp,
                    Status = "Failure",
                    ErrorMessage = ex.Message
                };
                var insertOperation = TableOperation.Insert(logEntry);
                await table.ExecuteAsync(insertOperation);
            }
        }

        private static async Task StoreLogInTable(DateTime timestamp)
        {
            var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference("WeatherLogs");
            await table.CreateIfNotExistsAsync();

            var logEntry = new WeatherLogEntity
            {
                PartitionKey = "WeatherLog",
                RowKey = timestamp.ToString("yyyyMMddHHmmss"),
                Timestamp = timestamp,
                Status = "Failure",
                ErrorMessage = ex.Message
            };
            var insertOperation = TableOperation.Insert(logEntry);
            await table.ExecuteAsync(insertOperation);
        }
    }
}

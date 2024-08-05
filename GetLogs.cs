namespace WeatherFunctionApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Azure.Cosmos.Table;
    using WeatherFunctionApp.Entities;

    public static class GetLogs
    {
        [FunctionName("GetLogs")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "logs")] HttpRequest req,
            ILogger log)
        {
            string start = req.Query["start"];
            string end = req.Query["end"];

            if (!DateTime.TryParse(start, out DateTime startTime) || !DateTime.TryParse(end, out DateTime endTime))
            {
                return new BadRequestObjectResult("Please provide valid start and end dates.");
            }

            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference("WeatherLogs");

            string filter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "WeatherLog"),
                TableOperators.And,
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, startTime),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, endTime)
                )
            );

            var query = new TableQuery<WeatherLogEntity>().Where(filter);
            var logs = new List<WeatherLogEntity>();

            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token);
                logs.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            return new OkObjectResult(logs);
        }
    }
}

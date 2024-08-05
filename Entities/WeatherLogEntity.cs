namespace WeatherFunctionApp.Entities
{
    using Microsoft.Azure.Cosmos.Table;
    public class WeatherLogEntity : TableEntity
    {
        public string Status { get; set; }
        public string BlobUri { get; set; }
        public string ErrorMessage { get; set; }
    }
}

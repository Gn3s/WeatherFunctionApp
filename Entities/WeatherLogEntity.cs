namespace WeatherFunctionApp.Entities
{
    using Microsoft.Azure.Cosmos.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WeatherLogEntity : TableEntity
    {
        public string Status { get; set; }
        public string BlobUri { get; set; }
        public string ErrorMessage { get; set; }
    }
}

namespace WeatherFunctionApp
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            // Add other services here
        }
    }
}

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

//*Register ServiceBus Client*//

var connectionSB = Environment.GetEnvironmentVariable("SB_Conn");
if (string.IsNullOrEmpty(connectionSB))
{
    throw new InvalidOperationException(
        "Please specify a valid Service Bus Connection String in the Azure Functions Settings or your local.settings.json file.");
}


#region ServiceBus Client injected into DI
// Register SB with Managed Identity //
//builder.Services.AddSingleton<ServiceBusClient>(provider =>
//{
    
//    var defaultCredential = new DefaultAzureCredential();
//    var serviceBusNamespace = Environment.GetEnvironmentVariable("connectionSB");// "eh-ase-servicebus-dev.servicebus.windows.net"; //without SASKey
//    return new ServiceBusClient(serviceBusNamespace, defaultCredential);

//});

//// Register SB with SAS Key, using AMQP as transport
builder.Services.AddSingleton((s) => {
    return new ServiceBusClient(connectionSB, new ServiceBusClientOptions() { TransportType = ServiceBusTransportType.AmqpWebSockets });
});
#endregion

builder.Build().Run();

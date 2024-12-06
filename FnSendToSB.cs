using Azure.Messaging.ServiceBus;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace DK_PoC_SendToSB_FN
{
    public class FnSendToSB
    {
        private readonly ILogger<FnSendToSB> _logger;
        private readonly ServiceBusClient _sbClient;
        public FnSendToSB(ILogger<FnSendToSB> logger, ServiceBusClient sbClient)
        {
            _logger = logger;
            _sbClient = sbClient;
        }

        [Function("FnSendToSB")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, FunctionContext functionContext)
        {
            _logger.LogInformation($"============ Function {functionContext.FunctionDefinition.Name} has started at {DateTime.Now} ============");

            var connectionSB = Environment.GetEnvironmentVariable("SB_Conn");
            var topicName = Environment.GetEnvironmentVariable("SB_TopicName");
            var OptId = System.Diagnostics.Activity.Current?.RootId;
            var operationName = string.Empty;
            var testid = string.Empty;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
                if (values.ContainsKey("id")) { testid = values["id"]; }
                if (values.ContainsKey("opt")) { operationName = values["opt"]; }

                switch (operationName.Trim().ToLower())
                {
                    case "send2sb":

                        await using (var sender = _sbClient.CreateSender(topicName))
                        {
                            var sbMessage = new ServiceBusMessage((new { fullName = "Deepak Shaw", 
                                                                         greetMsg = "all is well!!", 
                                                                         compId = 16, 
                                                                         date = DateTime.Now.ToString("dd-MMM-yy HH:mm:ss fff")}).ToString());
                            sbMessage.ContentType = "application/json";

                            //**SET SYSTEM  Property **//
                            sbMessage.ApplicationProperties.Add("Entity", operationName);
                            sbMessage.ApplicationProperties.Add("Owner", operationName);

                            //**SET CUSTOM  Property **//
                            sbMessage.SessionId = $"DK-POST {Guid.NewGuid()}";        
                            sbMessage.ReplyToSessionId = testid;
                            //* SET Label *//
                            sbMessage.Subject = $"Sub: {operationName}"; 
                            
                            await sender.SendMessageAsync(sbMessage);
                            if (!sender.IsClosed)
                                await sender.CloseAsync();
                        }
                        break;
                    default:
                        _logger.LogWarning($"{operationName.Trim()} is not allowed, however function execution completed.");
                        break;
                }

                _logger.LogInformation($"============ Function {functionContext.FunctionDefinition.Name} with ID [{OptId}] completed at {DateTime.Now} ============");
                return new OkObjectResult("DONE");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestResult();
            }
        }
    }
}

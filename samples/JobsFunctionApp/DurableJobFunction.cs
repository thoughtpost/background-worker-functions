using System;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Thoughtpost.Background;
using Thoughtpost.Background.Jobs;
using Thoughtpost.Background.Models;

namespace JobsFunctionApp
{
    public static class DurableJobFunction
    {

        [FunctionName("RunDurableJob")]
        public static async Task<ResponseModel> RunDurableJob(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log,
            ExecutionContext execContext)
        {
            var config = JobFunction.GetConfiguration(execContext);

            JobModel model = JsonConvert.DeserializeObject<JobModel>(context.GetInput<string>());

            ResponseModel response = await context.CallActivityAsync<ResponseModel>("RunJob", model);

            return response;
        }


        [FunctionName("RunDurableJobFromQueue")]
        public static async Task RunDurableJobFromQueue(
            [ServiceBusTrigger("durablejobs", Connection = "ServiceBusConnectionString")]string myQueueItem,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log,
            ExecutionContext context)
        {
            string instanceId = await starter.StartNewAsync("RunDurableJob", myQueueItem);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }


    }
}

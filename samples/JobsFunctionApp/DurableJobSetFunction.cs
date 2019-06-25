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

    public static class DurableJobSetFunction
    {

        [FunctionName("RunDurableJobSet")]
        public static async Task<ResponseModel> RunDurableJobSet(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log,
            ExecutionContext execContext)
        {
            var config = JobFunction.GetConfiguration(execContext);

            JobSetModel model = JsonConvert.DeserializeObject<JobSetModel>(context.GetInput<string>());

            ResponseModel response = new ResponseModel() { Id = model.Id };
            foreach ( JobModel job in model.Jobs )
            {
                job.Id = model.Id;
                response = await context.CallActivityAsync<ResponseModel>("RunJob", job);
            }

            return response;
        }


        [FunctionName("RunDurableJobSetFromQueue")]
        public static async Task RunDurableJobFromQueue(
            [ServiceBusTrigger("durablejobsets", Connection = "ServiceBusConnectionString")]string myQueueItem,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log,
            ExecutionContext context)
        {
            string instanceId = await starter.StartNewAsync("RunDurableJobSet", myQueueItem);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }


    }
}

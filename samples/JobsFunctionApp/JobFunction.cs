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
    public static class JobFunction
    {
        [FunctionName("RunJob")]
        public async static Task<ResponseModel> RunJob([ActivityTrigger] JobModel model, ILogger log,
            ExecutionContext context)
        {
            ResponseModel response = await JobFunction.InternalRun(model, GetJob(model), log, context);

            return response;
        }

        public static IBackgroundJob GetJob(JobModel model)
        {
            IBackgroundJob job = null;

            switch (model.Job.ToLower())
            {
                case "import":
                    {
                        job = new Thoughtpost.Background.Import.ImportCsvJob();
                    }
                    break;

                default:
                    {
                        job = new SleepJob();
                    }
                    break;
            }

            return job;
        }

        [FunctionName("jobqueue")]
        public async static Task RunFromQueue(
            [ServiceBusTrigger("jobs", Connection = "ServiceBusConnectionString")]string myQueueItem, 
            ILogger log,
            ExecutionContext context)
        {
            JobModel model = JsonConvert.DeserializeObject<JobModel>(myQueueItem);

            await JobFunction.InternalRun(model, GetJob(model), log, context);
        }

        [FunctionName("RunDurableJob")]
        public static async Task<ResponseModel> RunDurableJob(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log,
            ExecutionContext execContext)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(execContext.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

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

        public async static Task<ResponseModel> InternalRun(JobModel model,
            IBackgroundJob job,
            ILogger logger,
            ExecutionContext context)
        {
            logger.LogInformation($"Run of Job {model.Job} for ID = '{model.Id}'.");

            ResponseModel response = new ResponseModel()
            {
                Id = model.Id
            };

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                StatusRelay relay = new StatusRelay(config);
                await relay.Initialize();

                response = await job.Run(model, relay, logger, config );

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                logger.LogError(ex.StackTrace);

                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}

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
    public static class InteractiveJobFunction
    {
        [FunctionName("RunInteractiveJob")]
        public static async Task<ResponseModel> RunInteractiveJob(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log,
            ExecutionContext execContext)
        {
            var config = JobFunction.GetConfiguration(execContext);

            JobModel model = JsonConvert.DeserializeObject<JobModel>(context.GetInput<string>());

            ResponseModel response = await context.CallActivityAsync<ResponseModel>("RunJob", model);

            response.InstanceId = context.InstanceId;

            response = await context.CallActivityAsync<ResponseModel>("RequestHumanApproval", response);

            using (var timeoutCts = new System.Threading.CancellationTokenSource())
            {
                int seconds = Int32.Parse(response.Value);

                DateTime dueTime = context.CurrentUtcDateTime.AddSeconds(seconds);
                Task durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

                Task countdown = context.CallActivityAsync("Countdown", response );

                Task<ResponseModel> approvalEvent = context.WaitForExternalEvent<ResponseModel>("HumanApprovedEvent");

                if (approvalEvent == await Task.WhenAny(countdown, approvalEvent, durableTimeout))
                {
                    timeoutCts.Cancel();

                    await context.CallActivityAsync("JobApproved", response);
                }
                else
                {
                    await context.CallActivityAsync("JobEscalated", response);
                }
            }

            return response;
        }


        [FunctionName("RunInteractiveJobFromQueue")]
        public static async Task RunInteractiveJobFromQueue(
            [ServiceBusTrigger("interactivejobs", Connection = "ServiceBusConnectionString")]string myQueueItem,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log,
            ExecutionContext context)
        {
            string instanceId = await starter.StartNewAsync("RunInteractiveJob", myQueueItem);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }


        [FunctionName("RunHumanApprovalromQueue")]
        public static async Task RunHumanApprovalFromQueue(
            [ServiceBusTrigger("humanapproval", Connection = "ServiceBusConnectionString")]string myQueueItem,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log,
            ExecutionContext context)
        {
            ResponseModel model = JsonConvert.DeserializeObject<ResponseModel>(myQueueItem);

            await starter.RaiseEventAsync(model.InstanceId, "HumanApprovedEvent", model);
        }


        [FunctionName("RequestHumanApproval")]
        public async static Task<ResponseModel> RequestHumanApproval([ActivityTrigger] ResponseModel model,
            ILogger logger,
            ExecutionContext context)
        {
            try
            {
                StatusRelay relay = new StatusRelay(JobFunction.GetConfiguration(context));
                await relay.Initialize();

                model.Percent = 50;
                model.Message = "Awaiting human approval...";
                model.Value = "30";
                model.Complete = false;

                model.Url = "https://localhost:44309/home/approval/?id=" + model.Id + 
                    "&instanceId=" + model.InstanceId;

                await relay.SendStatusAsync(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                logger.LogError(ex.StackTrace);
            }

            return model;
        }

        [FunctionName("JobApproved")]
        public async static Task<ResponseModel> JobApproved([ActivityTrigger] ResponseModel model,
            ILogger logger,
            ExecutionContext context)
        {
            try
            {
                StatusRelay relay = new StatusRelay(JobFunction.GetConfiguration(context));
                await relay.Initialize();

                model.Success = true;
                model.Complete = true;
                model.Percent = 100;
                model.Result = "Job approval complete";
                model.Message = "Job approval complete";

                await relay.SendStatusAsync(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                logger.LogError(ex.StackTrace);
            }

            return model;
        }


        [FunctionName("JobEscalated")]
        public async static Task<ResponseModel> JobEscalated([ActivityTrigger] ResponseModel model,
            ILogger logger,
            ExecutionContext context)
        {
            try
            {
                StatusRelay relay = new StatusRelay(JobFunction.GetConfiguration(context));
                await relay.Initialize();

                model.Success = false;
                model.Complete = true;
                model.Percent = 100;
                model.Result = "Job approval ESCALATED";
                model.Message = "Job approval ESCALATED";

                await relay.SendStatusAsync(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                logger.LogError(ex.StackTrace);
            }

            return model;
        }
    }
}

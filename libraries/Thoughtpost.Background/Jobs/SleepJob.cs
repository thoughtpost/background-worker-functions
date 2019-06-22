using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Thoughtpost.Background.Models;

namespace Thoughtpost.Background.Jobs
{
    public class SleepJob : IBackgroundJob
    {
        public async Task<ResponseModel> Run(JobModel model, ILogger logger, StatusRelay relay)
        {
            ResponseModel response = new ResponseModel()
            {
                Id = model.Id
            };

            int sleep = 1000;
            int loop = 20;

            try
            {
                sleep = Int32.Parse(model.Parameters["Delay"]);
                loop = Int32.Parse(model.Parameters["Iterations"]);
            }
            catch ( Exception ex )
            {
                logger.LogError(ex.Message);
            }

            response.Percent = 0;
            response.Message = "Starting to get drowsy...";

            await relay.SendStatusAsync(response);

            for ( int i = 0; i <= loop; i++ )
            {
                System.Threading.Thread.Sleep(sleep);

                decimal dpct = ((decimal)i / (decimal)loop) * 100;
                int ipct = (int)dpct;

                response.Message = "Processing " + model.Details + "...";
                response.Percent = ipct;

                await relay.SendStatusAsync(response);
            }

            response.Complete = true;
            response.Success = true;
            response.Message = "Nap complete";

            await relay.SendStatusAsync(response);


            return response;
        }

    }
}

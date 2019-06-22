using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

using Thoughtpost.Background;
using Thoughtpost.Background.Jobs;
using Thoughtpost.Background.Models;

namespace JobsWeb
{
    public class BackgroundHub : Hub
    {
        public BackgroundHub(IConfiguration cfg)
        {
            this.Configuration = cfg;
        }

        public IConfiguration Configuration { get; set; }
        public async Task<ResponseModel> Subscribe(string id)
        {
            ResponseModel response = new ResponseModel()
            {
                Id = id
            };

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);

                StatusRelay relay = new StatusRelay(this.Configuration);
                await relay.Initialize();

                response = await relay.GetStatusAsync(id);
                if (response == null)
                {
                    response = new ResponseModel();
                    response.Id = id;
                }

                relay.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message, ex);
                Trace.TraceError(ex.StackTrace);

                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task StatusRelayUpdate(ResponseModel model)
        {
            try
            {
                StatusRelay relay = new StatusRelay(this.Configuration);
                await relay.Initialize();

                await relay.SetStatusAsync(model);

                relay.Dispose();

                await Clients.Group(model.Id).SendAsync("statusRelayUpdate", model);
            }
            catch ( Exception ex )
            {
                Trace.TraceError(ex.Message);
            }
        }

    }
}

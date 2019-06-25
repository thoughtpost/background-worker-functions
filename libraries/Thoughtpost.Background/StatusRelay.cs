using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;

using Thoughtpost.Azure;
using Thoughtpost.Background.Models;

namespace Thoughtpost.Background
{
    public class StatusRelay : IDisposable
    {
        protected IConfiguration config = null;
        protected HubConnection hubConnection = null;

        public StatusCache Cache { get; set; }

        public StatusRelayUpdateDelegate Update { get; set; }

        public StatusRelay(IConfiguration cfg)
        {
            this.config = cfg;
            this.hubConnection = null;
            this.Cache = null;
        }

        public async Task Initialize()
        {
            await Initialize(
                config["StatusRelayUrl"],
                config["StatusRelayHub"]);
        }

        public async Task Initialize(string url, string hub)
        {
            try
            {
                if ( this.hubConnection == null || this.hubConnection.State == HubConnectionState.Disconnected )
                {
                    this.hubConnection = new HubConnectionBuilder()
                                    .WithUrl(url)
                                    .Build();

                    this.hubConnection.On<ResponseModel>("statusRelayUpdate", (model) =>
                    {
                        if ( this.Update != null )
                        {
                            this.Update(model);
                        }
                    });

                    await hubConnection.StartAsync();
                }

            }
            catch ( Exception ex )
            {
                Trace.TraceError(ex.Message);

                if (this.hubConnection != null)
                {
                    this.hubConnection.StopAsync().Wait();
                    this.hubConnection = null;
                }
            }

            try
            {
                if (this.Cache == null )
                {
                    //RedisCacheOptions options = new RedisCacheOptions();
                    //options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(config["RedisConnectionString"]);

                    //IDistributedCache dc = new RedisCache(options);

                    BlobAsyncDistributedCache<ResponseModel> dc = new BlobAsyncDistributedCache<ResponseModel>(
                        this.config);
                    this.Cache = new StatusCache(dc);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);

                if (this.Cache != null)
                {
                    this.Cache.Dispose();
                    this.Cache = null;
                }
            }

        }

        public async Task<ResponseModel> GetStatusAsync(string id)
        {
            await Initialize();

            if (this.Cache != null)
            {
                return await this.Cache.GetAsync(id);
            }

            return null;
        }

        public async Task SetStatusAsync(ResponseModel model)
        {
            await Initialize();

            if (this.Cache != null)
            {
                await this.Cache.SetAsync(model);
            }
        }

        public async Task Subscribe(string id)
        {
            if (this.hubConnection != null)
            {
                await this.hubConnection.InvokeCoreAsync("subscribe", new object[] { id });
            }
        }

        public async Task SendStatusAsync(ResponseModel model)
        {
            await this.SetStatusAsync(model);

            if (this.hubConnection != null)
            {
                await this.hubConnection.InvokeCoreAsync("statusRelayUpdate", new object[] { model });
            }

        }

        public void Dispose()
        {
            if (this.Cache != null)
            {
                this.Cache.Dispose();
                this.Cache = null;
            }
            if (this.hubConnection != null)
            {
                this.hubConnection.StopAsync().Wait();
                this.hubConnection = null;
            }
        }

        public delegate void StatusRelayUpdateDelegate(ResponseModel model);
    }
}

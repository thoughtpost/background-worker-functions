using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using Microsoft.AspNetCore.SignalR.Client;

using Thoughtpost.Azure;
using Thoughtpost.Background;
using Thoughtpost.Background.Models;

namespace JobsConsoleApp
{
    class Program
    {
        static ResponseModel Model = null;

        static void StatusRelayUpdate(ResponseModel model)
        {
            Program.Model = model;

            Console.WriteLine(model.Message);
        }

        async static Task Main(string[] args)
        {
            Console.WriteLine("Job client from a console app");

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            StorageHelper<JobModel> storage = new StorageHelper<JobModel>(config);

            JobModel model = new JobModel();
            model.Id = "CONSOLE1";
            model.Job = "Sleep";
            model.Details = "Console sleeping";
            model.Path = "";
            model.Parameters = new System.Collections.Generic.Dictionary<string, string>();
            model.Parameters.Add("Iterations", "10");
            model.Parameters.Add("Delay", "3000");

            Model = new ResponseModel() { Id = model.Id };

            StatusRelay relay = new StatusRelay(config);
            await relay.Initialize();

            await relay.Subscribe(model.Id);

            relay.Update = StatusRelayUpdate;

            await storage.SaveToServiceBusQueue(model, "jobs");

            while (Model.Complete == false )
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

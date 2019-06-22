using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using Thoughtpost.Azure;
using Thoughtpost.Background;
using Thoughtpost.Background.Models;

namespace JobsConsoleApp
{
    class Program
    {
        async static Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            StorageHelper<JobModel> storage = new StorageHelper<JobModel>(config);

            JobModel model = new JobModel();
            model.Id = "999";
            model.Job = "Sleep";
            model.Details = "Power sleeping";
            model.Path = "XYZ";
            model.Parameters = new System.Collections.Generic.Dictionary<string, string>();
            model.Parameters.Add("Iterations", "10");
            model.Parameters.Add("Delay", "3000");

            await storage.SaveToServiceBusQueue(model, "jobs");
        }
    }
}

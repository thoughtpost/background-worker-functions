using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Thoughtpost.Azure;
using Thoughtpost.Background.Models;

namespace Thoughtpost.Background.Tests
{
    [TestClass]
    public class JobTests
    {
        [TestMethod]
        public async Task QueueJob()
        {
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

        [TestMethod]
        public async Task SetCachedModel()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //RedisCacheOptions options = new RedisCacheOptions();
            //options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(config["RedisConnectionString"]);

            //IDistributedCache dc = new RedisCache(options);
            BlobAsyncDistributedCache<ResponseModel> dc = new BlobAsyncDistributedCache<ResponseModel>("cache",
                config);

            StatusCache cache = new StatusCache(dc);

            ResponseModel model = new ResponseModel();
            model.Id = "999";
            model.Percent = 50;

            await cache.SetAsync(model);
        }

        [TestMethod]
        public async Task GetCachedModel()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //RedisCacheOptions options = new RedisCacheOptions();
            //options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(config["RedisConnectionString"]);

            //IDistributedCache dc = new RedisCache(options);
            BlobAsyncDistributedCache<ResponseModel> dc = new BlobAsyncDistributedCache<ResponseModel>("cache",
                config);

            StatusCache cache = new StatusCache(dc);

            ResponseModel model = await cache.GetAsync("999");
        }
    }
}

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Thoughtpost.Azure;
using Thoughtpost.Background.Models;

using Thoughtpost.Background.Import;

namespace Thoughtpost.Background.Tests
{
    [TestClass]
    public class SetupTests
    {
        [TestMethod]
        public async Task ClearAzureFunctionsStorage()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            AccountHelper account = new AccountHelper(config);

            await account.DeleteBlobContainer("azure-webjobs-hosts");
            await account.DeleteBlobContainer("azure-webjobs-secrets");
            await account.DeleteBlobContainer("durablefunctionshub-leases");

            await account.DeleteStorageQueue("durablefunctionshub-control-00");
            await account.DeleteStorageQueue("durablefunctionshub-control-01");
            await account.DeleteStorageQueue("durablefunctionshub-control-02");
            await account.DeleteStorageQueue("durablefunctionshub-control-03");
            await account.DeleteStorageQueue("durablefunctionshub-workitems");

            await account.DeleteTable("DurableFunctionsHubHistory");
            await account.DeleteTable("DurableFunctionsHubInstances");
        }

        [TestMethod]
        public async Task ClearJobCache()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            AccountHelper account = new AccountHelper(config);

            await account.DeleteBlobContainer("jobcache");
        }

    }
}

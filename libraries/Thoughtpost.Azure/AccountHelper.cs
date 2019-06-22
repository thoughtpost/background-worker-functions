
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

using Thoughtpost;
using Microsoft.Extensions.Configuration;

using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Core;

namespace Thoughtpost.Azure
{
    public class AccountHelper
    {
        // PUT YOUR CONNECTION STRINGS HERE
        protected static string ServiceBusConnectionString = "";
        protected static string StorageConnectionString = "";

        public AccountHelper(IConfiguration config)
        {
            ServiceBusConnectionString = config["ServiceBusConnectionString"];
            StorageConnectionString = config["StorageConnectionString"];
        }

        public Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient GetBlobClient()
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            return blobClient;
        }

        public CloudBlobContainer GetBlobContainer(string container)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);

            // Create the container if it doesn't already exist.
            blobContainer.CreateIfNotExistsAsync().Wait();

            blobContainer.SetPermissionsAsync(
                new BlobContainerPermissions
                {
                    PublicAccess =
                        BlobContainerPublicAccessType.Blob
                }).Wait();

            return blobContainer;
        }


        public QueueClient GetQueueClient(string queueName)
        {
            // Initialize the connection to Service Bus Queue
            QueueClient queueClient = new QueueClient(ServiceBusConnectionString, queueName);

            return queueClient;
        }

        public Microsoft.WindowsAzure.Storage.Table.CloudTable GetTable(string tableName)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Table.CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExistsAsync().Wait();

            return table;
        }

    }
}

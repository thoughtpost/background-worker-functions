using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

namespace Thoughtpost.Azure
{
    public class StorageHelper<T> where T : new()
    {
        public StorageHelper(IConfiguration config)
        {
            this.Account = new AccountHelper(config);
        }

        public AccountHelper Account { get; set; }


        public async Task SaveToServiceBusQueue(T entity, string queueName)
        {
            QueueClient client = Account.GetQueueClient(queueName);

            await SaveToServiceBusQueue(entity, client);
        }

        public async Task SaveToServiceBusQueue(T entity, QueueClient queue)
        {
            Message msg = new Message(ToBytes(entity));

            await queue.SendAsync(msg);
        }


        public static T FromJson(string json)
        {
            T entity = default(T);

            entity = JsonConvert.DeserializeObject<T>(json);

            return entity;
        }

        public static string ToJson(T entity)
        {
            string json = JsonConvert.SerializeObject(entity);

            return json;
        }

        public static byte[] ToBytes(T entity)
        {
            return System.Text.ASCIIEncoding.Default.GetBytes(ToJson(entity));
        }

        protected CloudBlobContainer GetContainer(string container)
        {
            return Account.GetBlobContainer(container);
        }

        public async Task<byte[]> Load(string containerName, string id, string ns)
        {
            byte[] buffer = null;

            try
            {
                CloudBlobContainer container = GetContainer(containerName);

                string name = id;
                if (string.IsNullOrEmpty(ns) == false)
                {
                    name = ns + "\\" + name;
                }

                // Retrieve reference to a blob named "myblob".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);

                bool exists = await blockBlob.ExistsAsync();
                if (exists == false) return null;

                MemoryStream ms = new MemoryStream();

                await blockBlob.DownloadToStreamAsync(ms);
                ms.Position = 0;

                buffer = new byte[ms.Length];

                ms.Read(buffer, 0, (int)ms.Length);
            }
            catch (Exception exCfg)
            {
                // No config is available
                throw exCfg;
            }

            return buffer;
        }


        public async Task Save(string containerName, Stream s, string id, string ns)
        {
            byte[] buffer = new byte[s.Length];
            s.Position = 0;
            s.Read(buffer, 0, buffer.Length);

            await Save(containerName, buffer, id, ns);
        }

        public async Task Save(string containerName, byte[] buffer, string id, string ns)
        {
            string name = id;
            if (string.IsNullOrEmpty(ns) == false)
            {
                name = ns + "\\" + name;
            }

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = GetContainer(containerName).GetBlockBlobReference(name);

            await blockBlob.UploadFromByteArrayAsync(buffer, 0, buffer.Length);
        }

        public async Task Delete(string containerName, string id, string ns)
        {
            string name = id;
            if (string.IsNullOrEmpty(ns) == false)
            {
                name = ns + "\\" + name;
            }

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = GetContainer(containerName).GetBlockBlobReference(name);

            await blockBlob.DeleteAsync();
        }
    }
}

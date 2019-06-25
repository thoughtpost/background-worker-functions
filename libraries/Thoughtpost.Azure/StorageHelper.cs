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
            QueueClient client = await Account.GetQueueClient(queueName);

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

        #region Table
        public Microsoft.WindowsAzure.Storage.Table.CloudTable GetTable(string tableName)
        {
            return this.Account.GetTable(tableName);
        }

        public async Task<TableResult> SaveToTable<E>(E entity, string tableName) where E : ITableEntity, new()
        {
            CloudTable t = Account.GetTable(tableName);

            return await SaveToTable<E>(entity, t);
        }

        public async Task<TableResult> SaveToTable<E>(E entity, CloudTable table) where E : ITableEntity, new()
        {
            TableOperation insertOp = TableOperation.InsertOrMerge(entity);

            return await table.ExecuteAsync(insertOp);
        }

        public async Task<TableResult> InternalSaveToTable<E>(E entity, string tableName) where E : ITableEntity, new()
        {
            CloudTable t = Account.GetTable(tableName);

            TableOperation insertOp = TableOperation.InsertOrMerge(entity);

            return await t.ExecuteAsync(insertOp);
        }

        public async Task<E> Get<E>(string pkey, string rkey, CloudTable table) where E : ITableEntity, new()
        {
            TableQuery<E> query = GetQuery<E>(pkey, rkey);
            List<E> list = await Get<E>(query, 1, table);
            if (list == null) return default(E);
            return list.FirstOrDefault();
        }

        public async Task<E> Get<E>(string pkey, string rkey, string tableName) where E : ITableEntity, new()
        {
            CloudTable t = Account.GetTable(tableName);
            TableQuery<E> query = GetQuery<E>(pkey, rkey);
            List<E> list = await Get<E>(query, 1, t);
            if (list == null) return default(E);
            return list.FirstOrDefault();
        }

        public async Task<E> Get<E>(E obj, string tableName) where E : ITableEntity, new()
        {
            CloudTable t = Account.GetTable(tableName);
            TableQuery<E> query = GetQuery<E>(obj.PartitionKey, obj.RowKey);
            List<E> list = await Get<E>(query, 1, t);
            if (list == null) return default(E);
            return list.FirstOrDefault();
        }


        public async Task<List<E>> Get<E>(string pkey, CloudTable table) where E : ITableEntity, new()
        {
            TableQuery<E> query = GetQuery<E>(pkey);
            List<E> list = await Get<E>(query, table);
            if (list == null) return new List<E>();
            return list;
        }

        public async Task<List<E>> Get<E>(TableQuery<E> rangeQuery, CloudTable table) where E : ITableEntity, new()
        {
            return await Get(rangeQuery, -1, table);
        }

        public async Task<List<E>> Get<E>(string pkey, string tableName) where E : ITableEntity, new()
        {
            CloudTable t = Account.GetTable(tableName);
            return await Get<E>(pkey, t);
        }

        public async Task<List<E>> Get<E>(TableQuery<E> rangeQuery, 
            int limit, CloudTable table) where E : ITableEntity, new()
        {
            int count = 0;
            List<E> results = new List<E>();

            if (limit > 0)
            {
                rangeQuery = rangeQuery.Take(limit);
            }

            TableContinuationToken token = null;
            TableQuerySegment<E> segment = await table.ExecuteQuerySegmentedAsync(rangeQuery, token);

            do
            {
                token = segment.ContinuationToken;

                // Loop through the results, displaying information about the entity.
                foreach (E entity in segment)
                {
                    results.Add(entity);

                    count++;
                    if (limit > 0 && count >= limit) break;
                }

            } while (token != null);

            return results;
        }

        protected TableQuery<E> GetQuery<E>(string pk, string rk) where E : ITableEntity, new()
        {
            // Create the table query.
            TableQuery<E> rangeQuery = new TableQuery<E>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)
                )
            );

            return rangeQuery;
        }

        protected TableQuery<E> GetQuery<E>(object key) where E : ITableEntity, new()
        {
            // Create the table query.
            TableQuery<E> rangeQuery = new TableQuery<E>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key.ToString())
            );

            return rangeQuery;
        }
        #endregion
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;

namespace Thoughtpost.Azure
{
    public class BlobAsyncDistributedCache<T> : IDistributedCache where T : new()
    {
        public BlobAsyncDistributedCache(string container, IConfiguration config)
        {
            this._container = container;
            this._helper = new StorageHelper<T>(config);
        }

        protected string _container = "cache";
        protected StorageHelper<T> _helper;

        public byte[] Get(string key)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            return _helper.Load(this._container, key, "");
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return _helper.Delete(this._container, key, "");
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, 
            CancellationToken token = default)
        {
            MemoryStream ms = new MemoryStream(value);
            return _helper.Save(this._container, ms, key, "");
        }
    }
}

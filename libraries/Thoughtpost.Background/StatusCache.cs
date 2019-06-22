using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

using Newtonsoft.Json;

using Thoughtpost.Background.Models;

namespace Thoughtpost.Background
{
    public class StatusCache : IDisposable
    {
        public StatusCache(IDistributedCache cache)
        {
            this.Cache = cache;
        }

        public IDistributedCache Cache { get; set; }

        public void Dispose()
        {
            if (this.Cache != null)
            {
                this.Cache = null;
            }
        }

        public async Task<ResponseModel> GetAsync(string id)
        {
            string json = await this.Cache.GetStringAsync(id);

            ResponseModel model = new ResponseModel() { Id = id };

            if (string.IsNullOrEmpty(json) == false) 
            {
                model = JsonConvert.DeserializeObject<ResponseModel>(json);
            }

            return model;
        }

        public async Task SetAsync(ResponseModel model)
        {
            string json = JsonConvert.SerializeObject(model);

            await this.Cache.SetStringAsync(model.Id, json);
        }
    }
}

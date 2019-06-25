using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Thoughtpost.Azure;
using Thoughtpost.Background;
using Thoughtpost.Background.Jobs;
using Thoughtpost.Background.Models;

using System.Linq;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;

namespace Thoughtpost.Background.Import
{
    public class ImportImageSearchJob : IBackgroundJob
    {
        public async Task<ResponseModel> Run(JobModel model, StatusRelay relay, ILogger logger, 
            IConfiguration configuration)
        {
            ResponseModel response = new ResponseModel() { Id = model.Id };

            response.Percent = 0;
            response.Message = "Reading table storage";

            try
            {
                string subscriptionKey = configuration["CognitiveServicesKey"];
                Images imageResults = null;
                var client = new ImageSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));
                client.Endpoint = "https://eastus2.api.cognitive.microsoft.com/";

                await relay.SendStatusAsync(response);

                StorageHelper<DynamicTableEntity> helper = new StorageHelper<DynamicTableEntity>(configuration);

                List<DynamicTableEntity> items = await helper.Get<DynamicTableEntity>(model.Id, "importdata");

                int lineCount = items.Count;
                int rowCount = 0;

                foreach ( DynamicTableEntity item in items )
                {
                    rowCount++;

                    decimal dpct = ((decimal)rowCount / (decimal)lineCount) * 100;
                    int ipct = (int)dpct;

                    response.Message = "Getting image for " + item.RowKey + "...";
                    response.Percent = ipct;

                    string search = item.GetValue(model.Path).ToString();

                    imageResults = await client.Images.SearchAsync(query: search);

                    if (imageResults != null && imageResults.Value.Count != 0)
                    {
                        var firstImageResult = imageResults.Value.First();

                        item["contentimageurl"] = firstImageResult.ContentUrl;
                        item["thumbnailimageurl"] = firstImageResult.ThumbnailUrl;
                    }
                    else
                    {
                        item["contentimageurl"] = "";
                        item["thumbnailimageurl"] = "";
                    }

                    await relay.SendStatusAsync(response);

                    await helper.SaveToTable<DynamicTableEntity>(item, "importdata");
                }

                response.Message = "Import complete";
                response.Complete = true;
                await relay.SendStatusAsync(response);

            }
            catch ( Exception ex )
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            finally
            {

            }

            return response;
        }
    }
}

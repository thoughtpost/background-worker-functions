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

namespace Thoughtpost.Background.Import
{
    public class ImportCsvJob : IBackgroundJob
    {
        public IEnumerable<string> ReadLines(Stream stream,
                                     Encoding encoding)
        {
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        public async Task<ResponseModel> Run(JobModel model, StatusRelay relay, ILogger logger, 
            IConfiguration configuration)
        {
            ResponseModel response = new ResponseModel() { Id = model.Id };

            response.Percent = 0;
            response.Message = "Importing file";

            try
            {
                await relay.SendStatusAsync(response);

                StorageHelper<DynamicTableEntity> helper = new StorageHelper<DynamicTableEntity>(configuration);

                byte[] csvdata = await helper.Load("importfiles", model.Path, "");

                MemoryStream ms = new MemoryStream(csvdata);
                string[] columns = null;

                List<string> lines = new List<string>(ReadLines(ms, Encoding.UTF8));

                int lineCount = lines.Count;
                int rowCount = 0;

                foreach (string line in lines)
                {
                    rowCount++;

                    if (columns == null)
                    {
                        columns = line.Split(',');
                        continue;
                    }

                    var values = line.Split(',');

                    DynamicTableEntity entity = new DynamicTableEntity();
                    for (int col = 0; col < columns.Length; col++)
                    {
                        entity[columns[col]] = values[col];
                    }

                    entity.PartitionKey = model.Id;
                    entity.RowKey = values[0];

                    decimal dpct = ((decimal)rowCount / (decimal)lineCount) * 100;
                    int ipct = (int)dpct;

                    response.Message = "Processing " + entity.RowKey + "...";
                    response.Percent = ipct;
                    await relay.SendStatusAsync(response);

                    await helper.SaveToTable<DynamicTableEntity>(entity, "importdata");
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Thoughtpost.Background.Models;

namespace Thoughtpost.Background.Jobs
{
    public interface IBackgroundJob
    {
        Task<ResponseModel> Run(JobModel model, ILogger logger, StatusRelay relay);
    }
}

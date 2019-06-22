using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JobsWeb.Models;

using Microsoft.Extensions.Configuration;

using Thoughtpost.Azure;
using Thoughtpost.Background;
using Thoughtpost.Background.Models;

namespace JobsWeb.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IConfiguration cfg)
        {
            this._config = cfg;
        }

        protected IConfiguration _config = null;

        public IActionResult Index(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "999";
            }
            DemoModel model = new DemoModel() { Id = id };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DemoModel model)
        {
            StorageHelper<JobModel> storage = new StorageHelper<JobModel>(_config);

            JobModel job = new JobModel();
            job.Id = model.Id;
            job.Job = "Sleep";
            job.Details = "Power sleeping";
            job.Path = "XYZ";
            job.Parameters = new System.Collections.Generic.Dictionary<string, string>();
            job.Parameters.Add("Iterations", "100");
            job.Parameters.Add("Delay", "500");

            await storage.SaveToServiceBusQueue(job, "durablejobs");

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

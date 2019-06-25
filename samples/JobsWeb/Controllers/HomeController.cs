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
        public async Task<IActionResult> Index(DemoModel model, string submit)
        {
            string queue = "durablejobs";

            JobModel job = new JobModel();
            job.Id = model.Id;

            switch (submit)
            {
                case "interactive":
                    {
                        job.Job = "Sleep";
                        job.Details = "Fast sleeping";
                        job.Parameters = new System.Collections.Generic.Dictionary<string, string>();
                        job.Parameters.Add("Iterations", "10");
                        job.Parameters.Add("Delay", "1000");

                        queue = "interactivejobs";
                        await QueueJob(job, queue);
                    }
                    break;

                case "jobset":
                    {
                        JobSetModel set = new JobSetModel();
                        set.Id = model.Id;

                        JobModel job1 = new JobModel();
                        job1.Job = "Sleep";
                        job1.Details = "Phase 1";
                        job1.Parameters = new System.Collections.Generic.Dictionary<string, string>();
                        job1.Parameters.Add("Iterations", "5");
                        job1.Parameters.Add("Delay", "1000");

                        JobModel job2 = new JobModel();
                        job2.Job = "Sleep";
                        job2.Details = "Phase 2";
                        job2.Parameters = new System.Collections.Generic.Dictionary<string, string>();
                        job2.Parameters.Add("Iterations", "10");
                        job2.Parameters.Add("Delay", "1000");

                        JobModel job3 = new JobModel();
                        job3.Job = "Sleep";
                        job3.Details = "Phase 3";
                        job3.Parameters = new System.Collections.Generic.Dictionary<string, string>();
                        job3.Parameters.Add("Iterations", "3");
                        job3.Parameters.Add("Delay", "5000");

                        set.Jobs = new List<JobModel>();
                        set.Jobs.Add(job1);
                        set.Jobs.Add(job2);
                        set.Jobs.Add(job3);

                        await QueueJobSet(set, "durablejobsets");
                    }
                    break;

                case "import":
                    {
                        job.Job = "Import";
                        job.Path = "Attendees.csv";
                        await QueueJob(job, queue);
                    }
                    break;

                case "images":
                    {
                        job.Job = "Images";
                        job.Path = "member_name";
                        await QueueJob(job, queue);
                    }
                    break;

                case "sleep":
                default:
                    {
                        job.Job = "Sleep";
                        job.Details = "Power sleeping";
                        job.Path = "XYZ";
                        job.Parameters = new System.Collections.Generic.Dictionary<string, string>();
                        job.Parameters.Add("Iterations", "100");
                        job.Parameters.Add("Delay", "500");
                        await QueueJob(job, queue);
                    }
                    break;
            }



            return View(model);
        }


        public async Task<IActionResult> Approval(string id, string instanceId)
        {
            ResponseModel model = new ResponseModel() { InstanceId = instanceId, Id = id };

            await QueueResponse(model, "humanapproval");

            return View(model);
        }


        protected async Task QueueResponse(ResponseModel model, string queue)
        {
            StorageHelper<ResponseModel> storage = new StorageHelper<ResponseModel>(_config);

            await storage.SaveToServiceBusQueue(model, queue);
        }

        protected async Task QueueJob(JobModel model, string queue)
        {
            StorageHelper<JobModel> storage = new StorageHelper<JobModel>(_config);

            await storage.SaveToServiceBusQueue(model, queue);
        }

        protected async Task QueueJobSet(JobSetModel model, string queue)
        {
            StorageHelper<JobSetModel> storage = new StorageHelper<JobSetModel>(_config);

            await storage.SaveToServiceBusQueue(model, queue);
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

        public async Task <IActionResult> BeforeAndAfter(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "999";
            }
            DemoModel model = new DemoModel() { Id = id };
            model.Attendees = new List<AttendeeModel>();

            StorageHelper<DynamicTableEntity> helper = new StorageHelper<DynamicTableEntity>(_config);

            List<DynamicTableEntity> attendees = await helper.Get<DynamicTableEntity>(id, 
                "importdata");
            foreach( DynamicTableEntity entity in attendees )
            {
                AttendeeModel am = new AttendeeModel();

                am.Name = entity.GetValue("member_name").ToString();
                am.BeforeImage = entity.GetValue("meetup_image").ToString();
                am.AfterImageThumbnail = entity.GetValue("thumbnailimageurl").ToString();
                am.AfterImage = entity.GetValue("contentimageurl").ToString();

                model.Attendees.Add(am);
            }
            return View(model);
        }


    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Thoughtpost.Background.Models
{
    public class JobSetModel
    {
        public string Id { get; set; }
        public List<JobModel> Jobs { get; set; }
    }
}

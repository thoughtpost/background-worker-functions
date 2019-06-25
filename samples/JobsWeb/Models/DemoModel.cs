using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobsWeb.Models
{
    public class DemoModel
    {
        public string Id { get; set; }

        public List<AttendeeModel> Attendees { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Thoughtpost.Background.Models
{
    public class JobModel
    {
        public string Id { get; set; }
        public string Job { get; set; }
        public string Details { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

}

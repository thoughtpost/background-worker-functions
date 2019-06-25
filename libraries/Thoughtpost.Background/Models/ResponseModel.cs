using System;
using System.Collections.Generic;
using System.Text;

namespace Thoughtpost.Background.Models
{
    public class ResponseModel
    {
        public ResponseModel()
        {
            this.Complete = false;
            this.Id = "noid";
        }
        public bool Success { get; set; }
        public bool Complete { get; set; }
        public string Message { get; set; }
        public string Value { get; set; }
        public string Url { get; set; }
        public string Result { get; set; }
        public int Percent { get; set; }
        public string Id { get; set; }

        public string InstanceId { get; set; }
    }
}

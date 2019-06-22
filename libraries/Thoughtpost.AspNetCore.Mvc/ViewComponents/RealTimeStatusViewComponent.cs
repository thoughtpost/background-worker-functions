using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Thoughtpost.Background.Models;

namespace Thoughtpost.AspNetCore.Mvc
{
    [ViewComponent(Name = "RealTimeStatus")]
    public class RealTimeStatusViewComponent : ViewComponent
    {
        public RealTimeStatusViewComponent()
        {

        }

        public IViewComponentResult Invoke(string method, string id, string style, string url,
            string onSuccess, string onError)
        {

            ResponseModel model = new ResponseModel()
            {
                Id = id,
                //Style = style,
                Url = url,
                //OnSuccess = onSuccess,
                //OnError = onError
            };

            return View(model);
        }


    }
}

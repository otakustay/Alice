using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Alice.Web.Infrastructure {
    public class NewtonJsonActionResult : ActionResult {
        private readonly object value;

        public NewtonJsonActionResult(object value) {
            this.value = value;
        }

        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.ContentEncoding = Encoding.UTF8;
            context.HttpContext.Response.ContentType = "application/json";

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            string text = JsonConvert.SerializeObject(value, Formatting.None, settings);

            context.HttpContext.Response.Write(text);
            context.HttpContext.Response.End();
        }
    }
}
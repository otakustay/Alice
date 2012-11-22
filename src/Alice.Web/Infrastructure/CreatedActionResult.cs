using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Infrastructure {
    public class CreatedActionResult : ActionResult {
        private readonly string resourceUrl;

        private readonly ActionResult actualResult;

        public CreatedActionResult(string resourceUrl, ActionResult actualResult) {
            this.resourceUrl = resourceUrl;
            this.actualResult = actualResult;
        }

        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.ContentEncoding = Encoding.UTF8;

            context.HttpContext.Response.StatusCode = 201;
            context.HttpContext.Response.RedirectLocation = resourceUrl;

            actualResult.ExecuteResult(context);
        }
    }
}
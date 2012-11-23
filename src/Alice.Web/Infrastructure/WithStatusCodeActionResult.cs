using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Infrastructure {
    public class WithStatusCodeActionResult : ActionResult {
        private readonly int statusCode;

        private readonly ActionResult actualResult;

        public WithStatusCodeActionResult(int statusCode, ActionResult actualResult) {
            this.statusCode = statusCode;
            this.actualResult = actualResult;
        }

        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.ContentEncoding = Encoding.UTF8;

            context.HttpContext.Response.StatusCode = statusCode;

            actualResult.ExecuteResult(context);
        }
    }
}
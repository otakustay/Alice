using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Infrastructure {
    public class CreatedActionResult : WithStatusCodeActionResult {
        private readonly string resourceUrl;

        public CreatedActionResult(string resourceUrl, ActionResult actualResult)
            : base(201, actualResult) {
            this.resourceUrl = resourceUrl;
        }

        public override void ExecuteResult(System.Web.Mvc.ControllerContext context) {
            context.HttpContext.Response.RedirectLocation = resourceUrl;

            base.ExecuteResult(context);
        }
    }
}
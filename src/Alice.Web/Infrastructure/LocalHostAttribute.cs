using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Infrastructure {
    public class LocalHostAttribute : FilterAttribute, IActionFilter {
        private static string[] localhostList = { "localhost", "127.0.0" };

        public void OnActionExecuted(ActionExecutedContext filterContext) {
            string host = filterContext.HttpContext.Request.Url.Host;
            bool isLocalHost = localhostList.Any(h => host.IndexOf(h, StringComparison.InvariantCultureIgnoreCase) >= 0);
            filterContext.Controller.ViewBag.IsLocalHost = isLocalHost;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext) {
        }
    }
}
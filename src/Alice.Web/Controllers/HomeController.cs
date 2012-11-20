using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Alice.Model;

namespace Alice.Web.Controllers {
    public class HomeController : Controller {
        //
        // GET: /Home/

        [HttpGet]
        public ActionResult Index() {
            return View();
        }

    }
}

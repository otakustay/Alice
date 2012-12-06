using Alice.Model;
using MarkdownDeep;
using NHibernate;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Controllers {
    public class ConsoleController : Controller {
        [Inject]
        public Markdown Transformer { get; set; }

        [Inject]
        public ISession DbSession { get; set; }

        public ActionResult Index() {
            string dir = Server.MapPath("~/Content");
            IEnumerable<FileInfo> files = new DirectoryInfo(dir)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTime);
            return View(files);
        }
    }
}

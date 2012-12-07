using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Alice.Web.Infrastructure;
using Ninject;
using Ninject.Web.Common;

namespace Alice.Web {
    public class MvcApplication : NinjectHttpApplication {

        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
            routes.IgnoreRoute("{*robots}", new { robots = @"(.*/)?robots.txt(/.*)?" });

            routes.MapRoute(
                "Rss2.0",
                "feed",
                new { controller = "Post", action = "Feed" }
            );

            routes.MapRoute(
                "Comments",
                "{postName}/comments",
                new { controller = "Post", action = "Comments" }
            );

            routes.MapRoute(
                "List",
                "{page}",
                new { controller = "Post", action = "List", page = 1 },
                new { page = @"\d+" }
            );

            routes.MapRoute(
                "View",
                "{name}",
                new { controller = "Post", action = "ViewPost" },
                new { name = @"^[^console]$" }
            );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Post", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected override IKernel CreateKernel() {
            IKernel kernel = new StandardKernel();
            kernel.Load(new AliceWebModule());

            return kernel;
        }

        protected override void OnApplicationStarted() {
            base.OnApplicationStarted();
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Alice.Web.Infrastructure {
    public class Rss20ActionResult : ActionResult {
        private readonly SyndicationFeed feed;

        public Rss20ActionResult(SyndicationFeed feed) {
            this.feed = feed;
        }

        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.ContentType = "application/rss+xml; charset=utf-8";

            Rss20FeedFormatter formatter = feed.GetRss20Formatter();
            using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Output)) {
                formatter.WriteTo(writer);
            }
        }
    }
}
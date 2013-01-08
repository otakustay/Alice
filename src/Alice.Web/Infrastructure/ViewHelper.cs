using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Infrastructure {
    public static class ViewHelper {
        private static Dictionary<string, int> version = new Dictionary<string, int>() {
            { "site-day.css", 1 }, { "site-night.css", 1 },
            { "blog-day.css", 1 }, { "blog-night.css", 1 },
            { "list-day.css", 1 }, { "list-night.css", 1 },
            { "post-day.css", 1 }, { "post-night.css", 1 },
            { "console-day.css", 1 }, { "console-night.css", 1 },
            { "jquery-1.8.2.js", 1 }, { "markdown.js", 1 },
            { "view-post.js", 1 }, { "opoa-blog.js", 2 }
        };

        public static string Style(this UrlHelper url, string name) {
            string themeType = Theme(null);
            string cssFilename = String.Format("{0}-{1}.css", name, themeType);

            return url.Content("~/styles/" + cssFilename + "?v=" + version[cssFilename]);
        }

        public static string Script(this UrlHelper url, string name) {
            string filename = name + ".js";
            return url.Content("~/scripts/" + filename + "?v=" + version[filename]);
        }

        public static string Theme(this HtmlHelper html) {
            int hour = DateTime.Now.Hour;
            return (hour >= 6 && hour < 18) ? "day" : "night";
        }
    }
}
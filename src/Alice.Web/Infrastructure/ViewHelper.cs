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
            { "console-day.css", 1 }, { "console-night.css", 1 }
        };

        public static string Style(this UrlHelper url, string name) {
            int hour = DateTime.Now.Hour;
            string themeType = (hour >= 6 && hour <= 18) ? "day" : "night";
            string cssFilename = String.Format("{0}-{1}.css", name, themeType);

            return url.Content("~/styles/" + cssFilename + "?v=" + version[cssFilename]);
        }
    }
}
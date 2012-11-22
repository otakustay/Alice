using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using MarkdownDeep;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;

namespace Alice.Web.Infrastructure {
    public class AliceWebModule : NinjectModule {
        public override void Load() {
            Bind<int>().ToConstant(10).Named("PageSize");

            string baseUrl = "http://otakustay.com";
            Bind<string>().ToConstant(baseUrl).Named("BaseUrl");

            SyndicationPerson me = 
                new SyndicationPerson("otakustay@live.com", "otakustay", "http://otakustay.com");
            Bind<SyndicationPerson>().ToConstant(me);

            Markdown markdownForPost = new Markdown() {
                ExtraMode = true,
                NewWindowForExternalLinks = true,
                PrepareImage = (tag, tiled) => {
                    string src = tag.attributes["src"];
                    if (src.StartsWith("/")) {
                        tag.attributes["src"] = baseUrl + src;
                    }
                    return true;
                },
                PrepareLink = (tag) => {
                    string href = tag.attributes["href"];
                    if (href.StartsWith("/")) {
                        tag.attributes["href"] = baseUrl + href;
                    }
                    return true;
                }
            };
            Bind<Markdown>().ToConstant(markdownForPost);

            string connectionString = ConfigurationManager.ConnectionStrings["MySql"].ConnectionString;
            Bind<string>().ToConstant(connectionString).Named("ConnectionString");
        }
    }
}
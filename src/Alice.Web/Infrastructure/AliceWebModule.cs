using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using Alice.Web.Models.Mapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using MarkdownDeep;
using NHibernate;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;

namespace Alice.Web.Infrastructure {
    public class AliceWebModule : NinjectModule {
        private static readonly ISessionFactory sessionFactory;

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

            Bind<ISession>().ToMethod(OpenSession).InTransientScope();
        }

        private static ISession OpenSession(IContext context) {
            ISession session = HttpContext.Current.Items["NHibernateSession"] as ISession;
            if (session == null) {
                session = sessionFactory.OpenSession();
                HttpContext.Current.Items["NHibernateSession"] = session;
            }
            return session;
        }

        static AliceWebModule() {
            string connectionString = ConfigurationManager.ConnectionStrings["MySql"].ConnectionString;
            sessionFactory = Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString(connectionString).ShowSql())
                .Mappings(m => m.FluentMappings.Add<PostExcerptEntityMap>())
                .Mappings(m => m.FluentMappings.Add<PostEntryEntityMap>())
                .Mappings(m => m.FluentMappings.Add<CommentEntityMap>())
                .Mappings(m => m.FluentMappings.Add<CommentAuthorComponentMap>())
                .BuildSessionFactory();
        }
    }
}
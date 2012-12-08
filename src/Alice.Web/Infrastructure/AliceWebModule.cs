using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using Alice.Web.Models.Mapping;
using MarkdownDeep;
using NHibernate;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Configuration = NHibernate.Cfg.Configuration;
using Lucene.Net.Index;
using System.IO;
using Lucene.Net.Store;

namespace Alice.Web.Infrastructure {
    public class AliceWebModule : NinjectModule {
        private static readonly ISessionFactory sessionFactory;

        public override void Load() {
            Bind<int>().ToConstant(10).Named("PageSize");
            Bind<string>().ToConstant("06-84-B9-E5-98-A9-64-CE-7B-A1-3F-FD-58-0A-12-6E").Named("PasswordHash");

            string baseUrl = "http://otakustay.com";
            Bind<string>().ToConstant(baseUrl).Named("BaseUrl");

            SyndicationPerson me = 
                new SyndicationPerson("otakustay@live.com", "otakustay", "http://otakustay.com");
            Bind<SyndicationPerson>().ToConstant(me);

            Bind<Markdown>().ToMethod(CreateMarkdownTransformer).InTransientScope();

            string connectionString = ConfigurationManager.ConnectionStrings["MySql"].ConnectionString;
            Bind<string>().ToConstant(connectionString).Named("ConnectionString");

            Bind<ISession>().ToMethod(OpenSession).InTransientScope();

            Bind<IndexWriter>().ToMethod(CreateIndexWriter).InTransientScope();
        }

        private static Markdown CreateMarkdownTransformer(IContext context) {
            string baseUrl = context.Kernel.Get<string>("BaseUrl");
            return new Markdown() {
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
        }

        private static ISession OpenSession(IContext context) {
            ISession session = HttpContext.Current.Items["NHibernateSession"] as ISession;
            if (session == null) {
                session = sessionFactory.OpenSession();
                HttpContext.Current.Items["NHibernateSession"] = session;
            }
            return session;
        }

        private IndexWriter CreateIndexWriter(IContext arg) {
            IndexWriter writer = new IndexWriter(
                FSDirectory.Open(new DirectoryInfo(@"f:\database\lucene")),
                new PanGuAnalyzer(),
                false,
                IndexWriter.MaxFieldLength.UNLIMITED
            );

            return writer;
        }

        static AliceWebModule() {
            string connectionString = ConfigurationManager.ConnectionStrings["MySql"].ConnectionString;
            sessionFactory = new Configuration()
                .SetProperty("dialect", "NHibernate.Dialect.MySQL5Dialect")
                .SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider")
                .SetProperty("connection.driver_class", "NHibernate.Driver.MySqlDataDriver")
                .SetProperty("connection.connection_string", connectionString)
                .AddAssembly("Alice.Web")
                .BuildSessionFactory();
        }
    }
}
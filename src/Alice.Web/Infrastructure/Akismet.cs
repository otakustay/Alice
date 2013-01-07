using Alice.Model;
using MarkdownDeep;
using NHibernate;
using Ninject;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;

namespace Alice.Web.Infrastructure {
    public class Akismet : IDisposable {
        private readonly string restUrl;

        private readonly string baseUrl;

        private readonly Markdown transformer;

        private readonly ISession session;

        public Akismet(
            [Named("BaseUrl")] string baseUrl, string apiKey, 
            [Named("Safe")] Markdown transformer, ISession session) {
            this.restUrl = String.Format("http://{0}.rest.akismet.com/1.1/comment-check", apiKey);
            this.baseUrl = baseUrl;
            this.transformer = transformer;
            this.session = session;
        }

        public void AuditComment(Comment comment) {
            NameValueCollection parameters = HttpUtility.ParseQueryString(String.Empty);
            parameters.Add("blog", baseUrl + "/");
            parameters.Add("user_ip", comment.Author.IpAddress);
            parameters.Add("user_agent", comment.Author.UserAgent);
            parameters.Add("referrer", comment.Referrer);
            parameters.Add("permalink", String.Format("{0}/{1}/", baseUrl, comment.PostName));
            parameters.Add("comment_type", "comment");
            parameters.Add("comment_author", comment.Author.Name);
            parameters.Add("comment_author_email", comment.Author.Email);
            parameters.Add("comment_content", transformer.Transform(comment.Content));

            string post = parameters.ToString();
            using (WebClient client = new WebClient()) {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                client.Headers.Add("User-Agent", "otakustay/1.0.1 | Akismet/2.5.3");
                string result = client.UploadString(restUrl, post);
                if (result == "true") {
                    comment.Audited = false;
                    session.Update(comment);
                }
            }
        }

        public void Dispose() {
            if (session != null) {
                session.Flush();
                session.Dispose();
            }
        }
    }
}
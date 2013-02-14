using Alice.Model;
using MarkdownDeep;
using NHibernate;
using Ninject;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace Alice.Web.Infrastructure {
    public class CommentProcessor : IDisposable {
        private readonly string restUrl;

        private readonly string baseUrl;

        private readonly Markdown transformer;

        private readonly ISession session;

        private readonly string siteName;

        private readonly string email;

        private readonly string template =
            File.ReadAllText(HostingEnvironment.MapPath("~/Views/_ReplyMailTemplate.tpl"));

        public CommentProcessor(
            [Named("Email")] string email, [Named("SiteName")] string siteName,
            [Named("BaseUrl")] string baseUrl, string apiKey,
            [Named("Safe")] Markdown transformer, ISession session) {
            this.email = email;
            this.siteName = siteName;
            this.restUrl = String.Format("http://{0}.rest.akismet.com/1.1/comment-check", apiKey);
            this.baseUrl = baseUrl;
            this.transformer = transformer;
            this.session = session;
        }

        private void AuditComment(Comment comment) {
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

            bool isSpam = CheckSpam(parameters.ToString());

            if (!isSpam) {
                parameters.Set("comment_content", comment.Content);
                isSpam = CheckSpam(parameters.ToString());
            }

            if (!isSpam) {
                // 至少要有一个非ASCII字符
                isSpam = comment.Content.All(c => c <= 255);
            }

            if (isSpam) {
                comment.Audited = false;
                session.Update(comment);
            }
        }

        private bool CheckSpam(string parameters) {
            using (WebClient client = new WebClient()) {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                client.Headers.Add("User-Agent", "otakustay/1.0.1 | Akismet/2.5.3");
                string result = client.UploadString(restUrl, parameters);
                return result == "true";
            }
        }

        private void NotifyReplyTarget(Comment comment) {
            Comment replyTarget = session.Get<Comment>(comment.Target.Value);
            PostExcerpt post = session.Get<PostExcerpt>(comment.PostName);
            if (replyTarget != null) {
                MailMessage message = new MailMessage(
                    new MailAddress(email, siteName),
                    new MailAddress(replyTarget.Author.Email, replyTarget.Author.Name)
                );
                message.Subject = String.Format("你在 {0} 的评论收到了回复", post.Title);
                message.Body = String.Format(
                    template,
                    replyTarget.Author.Name, post.Title,
                    transformer.Transform(replyTarget.Content),
                    comment.Author.Name,
                    transformer.Transform(comment.Content),
                    baseUrl, post.Name, comment.Id
                );
                message.IsBodyHtml = true;
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                using (SmtpClient client = new SmtpClient()) {
                    client.Send(message);
                }
            }
        }

        public void Process(Comment comment) {
            AuditComment(comment);
            if (comment.Audited && comment.Target.HasValue) {
                NotifyReplyTarget(comment);
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
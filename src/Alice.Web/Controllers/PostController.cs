using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Alice.Model;
using MarkdownDeep;
using MySql.Data.MySqlClient;
using System.ServiceModel.Syndication;
using Alice.Web.Infrastructure;
using Ninject;
using NHibernate;

namespace Alice.Web.Controllers {
    public class PostController : Controller {
        [Inject, Named("PageSize")]
        public int PageSize { get; set; }

        [Inject, Named("BaseUrl")]
        public string BaseUrl { get; set; }

        [Inject, Named("ConnectionString")]
        public string ConnectionString { get; set; }

        [Inject]
        public SyndicationPerson Author { get; set; }

        [Inject]
        public Markdown Transformer { get; set; }

        [Inject]
        public ISession DbSession { get; set; }

        [HttpGet]
        public ActionResult List(int page = 1) {
            if (page <= 0) {
                page = 1;
            }
            int start = (page - 1) * PageSize;

            int count = DbSession.QueryOver<PostExcerpt>().RowCount();
            IList<PostExcerpt> excerpts = DbSession.QueryOver<PostExcerpt>()
                .OrderBy(p => p.PostDate).Desc
                .Skip(start)
                .Take(PageSize)
                .List();

            ViewBag.Title = "宅居 - 宅并技术着";
            ViewBag.PageCount = (int)Math.Ceiling((double)count / (double)PageSize);
            ViewBag.PageIndex = page;
            return View(excerpts.Select(RenderExcerpt));
        }

        [HttpGet]
        public ActionResult ViewPost(string name) {
            PostEntry entry = DbSession.QueryOver<PostEntry>()
                .Where(p => p.Name == name)
                .SingleOrDefault();

            if (entry == null) {
                return new HttpStatusCodeResult(404);
            }
            else {
                return View(RenderEntry(entry));
            }
        }

        [ActionName("Comments")]
        [HttpGet]
        public ActionResult GetComments(string postName) {
            IEnumerable<Comment> comments = DbSession.QueryOver<Comment>()
                .Where(c => c.PostName == postName)
                .OrderBy(c => c.PostTime).Asc
                .List()
                .Select(RenderComment);

            if (Request.Headers["Accept"].Contains("application/json")) {
                return new NewtonJsonActionResult(comments);
            }
            else {
                return View(comments);
            }
        }

        [ActionName("Comments")]
        [HttpPost]
        public ActionResult PostComment(Comment comment) {
            comment.PostTime = DateTime.Now;
            comment.Author.IpAddress = Request.UserHostAddress;
            comment.Author.UserAgent = Request.UserAgent;

            DbSession.Save(comment);

            if (Request.Headers["Accept"].Contains("application/json")) {
                return new CreatedActionResult(
                    Url.Content("~/" + comment.PostName),
                    new NewtonJsonActionResult(comment)
                );
            }
            else {
                return Redirect(Url.Content("~/" + comment.PostName));
            }
        }

        [HttpGet]
        public ActionResult Feed() {
            IList<PostEntry> entries = DbSession.QueryOver<PostEntry>()
                .OrderBy(p => p.PostDate).Desc
                .Take(PageSize)
                .List();

            SyndicationFeed feed = new SyndicationFeed(
                "宅居",
                "宅并技术着",
                new Uri("http://otakustay.com/"),
                "otakustay-feed",
                new DateTimeOffset(entries[0].PostDate),
                entries.Select(TransformPost)
            );
            feed.Authors.Add(Author.Clone());

            return new Rss20ActionResult(feed);
        }

        private SyndicationItem TransformPost(PostEntry entry) {
            entry = RenderEntry(entry);
            SyndicationItem item = new SyndicationItem();
            item.Id = entry.Name;
            item.Title = SyndicationContent.CreatePlaintextContent(entry.Title);
            item.Content = SyndicationContent.CreateHtmlContent(Transformer.Transform(entry.Content));
            item.AddPermalink(new Uri("http://otakustay.com/" + entry.Name));
            item.PublishDate = new DateTimeOffset(entry.PostDate);
            item.LastUpdatedTime = new DateTimeOffset(entry.UpdateDate);
            item.Authors.Add(Author.Clone());
            return item;
        }

        private PostExcerpt RenderExcerpt(PostExcerpt excerpt) {
            excerpt.Excerpt = Transformer.Transform(excerpt.Excerpt);
            return excerpt;
        }

        private PostEntry RenderEntry(PostEntry entry) {
            entry.Content = Transformer.Transform(entry.Content);
            return entry;
        }

        private Comment RenderComment(Comment comment) {
            // TODO: 替换Transformer为SafeMode
            comment.Content = Transformer.Transform(comment.Content);
            return comment;
        }
    }
}

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
using System.Globalization;
using System.Text.RegularExpressions;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using NHibernate.Criterion;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Alice.Web.Models;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Alice.Web.Controllers {
    public class PostController : Controller {
        private static readonly Regex stopWords = new Regex(@"[!""\\:\[\]\{\}\(\)\^\+]", RegexOptions.Compiled);

        private static readonly Regex whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> validationMessages = new Dictionary<string, string>() {
            { "name", "请正确填写昵称" },
            { "email", "请正确填写邮箱" },
            { "content", "请填写内容" }
        };

        private static readonly Regex emailRule = new Regex(
            @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        [Inject, Named("PageSize")]
        public int PageSize { get; set; }

        [Inject, Named("BaseUrl")]
        public string BaseUrl { get; set; }

        [Inject, Named("ConnectionString")]
        public string ConnectionString { get; set; }

        [Inject]
        public SyndicationPerson Author { get; set; }

        [Inject]
        [Named("Full")]
        public Markdown Transformer { get; set; }

        [Inject]
        [Named("Safe")]
        public Markdown SafeTransformer { get; set; }

        [Inject]
        public ISession DbSession { get; set; }

        [Inject]
        public IKernel Kernel { get; set; }

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
                ViewBag.Title = String.Format("{0} - 宅居 - 宅并技术着", entry.Title);
                return View("ViewPost", RenderEntry(entry));
            }
        }

        [HttpGet]
        public ActionResult ReplyToComment(string postName, int target) {
            Comment comment = DbSession.Get<Comment>(target);
            if (comment == null) {
                return Redirect(Url.Content("~/" + postName + "/#post-comment"));
            }
            ViewBag.ReplyTarget = comment;
            return ViewPost(postName);
        }

        [ChildActionOnly]
        public ActionResult CommentList(string postName) {
            if (!ControllerContext.IsChildAction) {
                return Redirect(Url.Content("~/" + postName + "/"));
            }

            IEnumerable<Comment> comments = DbSession.QueryOver<Comment>()
                .Where(c => c.PostName == postName)
                .Where(c => c.Audited)
                .OrderBy(c => c.PostTime).Asc
                .List();
            List<CommentView> model = new List<CommentView>();
            Dictionary<int, string> authors = new Dictionary<int, string>();
            foreach (Comment comment in comments) {
                authors[comment.Id] = comment.Author.Name;
                CommentView view = RenderComment(comment, authors);
                model.Add(view);
            }

            return View("CommentList", model);
        }

        [ActionName("Comments")]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult PostComment(Comment comment) {
            comment.Author.Name = comment.Author.Name.Trim();
            comment.Author.Email = comment.Author.Email.Trim();

            // 验证
            if (String.IsNullOrEmpty(comment.Author.Name) ||
                comment.Author.Name.Length > 60) {
                ModelState.AddModelError("name", validationMessages["name"]);
            }
            if (String.IsNullOrEmpty(comment.Author.Email) ||
                comment.Author.Email.Length > 100 ||
                !emailRule.IsMatch(comment.Author.Email)) {
                ModelState.AddModelError("email", validationMessages["email"]);
            }
            if (String.IsNullOrEmpty(comment.Content.Trim())) {
                ModelState.AddModelError("content", validationMessages["content"]);
            }

            if (!ModelState.IsValid) {
                if (Request.IsAjaxRequest()) {
                    Dictionary<string, string> result = ModelState
                        .Where(m => m.Value.Errors.Any())
                        .ToDictionary(m => m.Key, m => m.Value.Errors[0].ErrorMessage);
                    return new NewtonJsonActionResult(result);
                }
                else {
                    PostEntry entry = DbSession.QueryOver<PostEntry>()
                        .Where(p => p.Name == comment.PostName)
                        .SingleOrDefault();
                    entry = RenderEntry(entry);
                    ViewBag.Comment = comment;
                    ViewBag.Title = String.Format("{0} - 宅居 - 宅并技术着", entry.Title);
                    return View("ViewPost", entry);
                }
            }

            comment.PostTime = DateTime.Now;
            comment.Author.IpAddress = Request.UserHostAddress;
            comment.Author.UserAgent = Request.UserAgent;
            comment.Audited = true; // 默认为已审核，有需要的再屏蔽
            comment.Referrer = Request.UrlReferrer == null ? String.Empty : Request.UrlReferrer.ToString();
            if (comment.Referrer.Length > 200) {
                comment.Referrer = comment.Referrer.Substring(0, 200);
            }

            Dictionary<int, string> targetAuthor = new Dictionary<int, string>();
            if (comment.Target.HasValue) {
                Comment target = DbSession.Get<Comment>(comment.Target);
                targetAuthor[comment.Target.Value] = target.Author.Name;
                if (target == null) {
                    comment.Target = null;
                }
            }

            DbSession.Save(comment);

            // 审核评论要有网络交互，异步进行不影响用户收到响应
            Task task = new Task(() => AuditComment(comment, Kernel)); ;
            task.Start();

            if (Request.IsAjaxRequest()) {
                CommentView commentView = RenderComment(comment, targetAuthor);
                ViewResult view = View("Comment", commentView);
                return new CreatedActionResult(Url.Content("~/" + comment.PostName), view);
            }
            else {
                return Redirect(Url.Content("~/" + comment.PostName + "/"));
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

        [HttpGet]
        [ValidateInput(false)]
        public ActionResult Search(string keywords, int page = 1) {
            if (keywords == null) {
                keywords = String.Empty;
            }
            keywords = keywords.Trim();
            if (keywords.Length == 0) {
                return Redirect(Url.Content("~/"));
            }

            string safeKeywords = stopWords.Replace(keywords, " ");
            using (IndexSearcher searcher = Kernel.Get<IndexSearcher>()) {
                BooleanQuery criteria = new BooleanQuery();
                QueryParser nameParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Name", new PanGuAnalyzer());
                Query nameQuery = nameParser.Parse(safeKeywords);
                nameQuery.Boost = 10000;
                string[] fields = { "Name", "Title", "Content", "Tags" };
                MultiFieldQueryParser parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, fields, new PanGuAnalyzer());
                Query query = parser.Parse(safeKeywords);
                criteria.Add(nameQuery, Occur.SHOULD);
                criteria.Add(query, Occur.SHOULD);

                int total;
                IList<PostExcerpt> posts = RetrievePostsFromIndexer(page, searcher, criteria, out total);

                ViewBag.Title = "检索 - " + keywords + " - 宅居 - 宅并技术着";
                ViewBag.SearchKeywords = keywords;
                ViewBag.PageCount = (int)Math.Ceiling((double)total / (double)PageSize);
                ViewBag.PageIndex = page;
                ViewBag.BaseUrl = Url.Content("~/search/" + keywords + "/");
                return View("List", posts.Select(RenderExcerpt));
            }
        }

        [HttpGet]
        [ValidateInput(false)]
        public ActionResult Tag(string tag, int page = 1) {
            if (tag == null) {
                tag = String.Empty;
            }
            tag = tag.Trim();
            if (tag.Length == 0) {
                return Redirect(Url.Content("~/"));
            }
            string[] tags = whitespace.Split(tag);
            tag = tags[0];
            if (tags.Length > 1) {
                return Redirect(Url.Content("~/tag/" + HttpUtility.UrlEncode(tag) + "/"));
            }

            tag = tag.ToLower();
            using (IndexSearcher searcher = Kernel.Get<IndexSearcher>()) {
                Query criteria = new TermQuery(new Term("Tags", tag));

                int total;
                IList<PostExcerpt> posts = RetrievePostsFromIndexer(page, searcher, criteria, out total);

                ViewBag.Title = "标签 - " + tag + " - 宅居 - 宅并技术着";
                ViewBag.PageCount = (int)Math.Ceiling((double)total / (double)PageSize);
                ViewBag.PageIndex = page;
                ViewBag.BaseUrl = Url.Content("~/tag/" + tag + "/");
                return View("List", posts.Select(RenderExcerpt));
            }
        }

        [ChildActionOnly]
        public ActionResult TagCloud() {
            IEnumerable<Tag> tags = DbSession.QueryOver<Tag>().List();
            ViewBag.MaxHitCount = (double)tags.Max(t => t.HitCount);
            return View(tags);
        }

        [HttpGet]
        public ActionResult Archive(int year, int month, int? page = 1) {
            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1);
            IQueryOver<PostExcerpt, PostExcerpt> query = DbSession.QueryOver<PostExcerpt>()
                .Where(p => p.PostDate >= startDate)
                .And(p => p.PostDate < endDate);
            int start = (page.Value - 1) * PageSize;

            int count = query.RowCount();
            IList<PostExcerpt> excerpts = query
                .OrderBy(p => p.PostDate).Desc
                .Skip(start)
                .Take(PageSize)
                .List();


            ViewBag.Title = String.Format("{0}年{1}月归档 - 宅居 - 宅并技术着", year, month);
            ViewBag.PageCount = (int)Math.Ceiling((double)count / (double)PageSize);
            ViewBag.PageIndex = page;
            ViewBag.BaseUrl = Url.Content(String.Format("~/archive/{0}/{1}/", year, month));
            return View("List", excerpts.Select(RenderExcerpt));
        }

        [ChildActionOnly]
        public ActionResult ArchiveList() {
            Dictionary<DateTime, int> archive = DbSession.CreateCriteria<PostExcerpt>()
                .SetProjection(Projections.Property<PostExcerpt>(p => p.PostDate))
                .List<DateTime>()
                .GroupBy(d => new DateTime(d.Year, d.Month, 1))
                .OrderByDescending(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            return View("ArchiveList", archive);
        }

        private SyndicationItem TransformPost(PostEntry entry) {
            entry = RenderEntry(entry);
            SyndicationItem item = new SyndicationItem();
            item.Id = entry.Name;
            item.Title = SyndicationContent.CreatePlaintextContent(entry.Title);
            item.Content = SyndicationContent.CreateHtmlContent(Transformer.Transform(entry.Content));
            item.AddPermalink(new Uri("http://otakustay.com/" + entry.Name + "/"));
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

        private CommentView RenderComment(Comment comment, IDictionary<int, string> authors) {
            CommentView view = new CommentView() {
                Id = comment.Id,
                Author = comment.Author,
                Content = SafeTransformer.Transform(comment.Content),
                PostName = comment.PostName,
                PostTime = comment.PostTime,
                Target = comment.Target
            };
            if (view.Target.HasValue) {
                view.TargetAuthorName = authors[view.Target.Value];
            }
            return view;
        }

        private IList<PostExcerpt> RetrievePostsFromIndexer(int page, IndexSearcher searcher, Query criteria, out int total) {
            TopDocs docs = searcher.Search(criteria, 100);

            string[] names = docs.ScoreDocs
                .Select(d => searcher.Doc(d.Doc).GetField("Name").StringValue)
                .ToArray();
            total = names.Length;

            IEnumerable<string> pagedNames = names
                .Skip((page - 1) * PageSize)
                .Take(PageSize);

            return DbSession.QueryOver<PostExcerpt>()
                .Where(Restrictions.InG("Name", pagedNames))
                .List();
        }

        private static void AuditComment(Comment comment, IKernel Kernel) {
            using (Akismet akismet = Kernel.Get<Akismet>()) {
                akismet.AuditComment(comment);
            }
        }
    }
}

using Alice.Model;
using Lucene.Net.Index;
using MarkdownDeep;
using NHibernate;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Alice.Web.Controllers {
    public class ConsoleController : Controller {
        [Inject]
        [Named("PasswordHash")]
        public string PasswordHash { get; set; }

        [Inject]
        public ISession DbSession { get; set; }

        [Inject]
        public IndexWriter Indexer { get; set; }

        [HttpGet]
        public ActionResult Login() {
            ViewBag.Title = "验证身份";

            return View();
        }

        [HttpPost]
        public ActionResult Login(string password) {
            ViewBag.Title = "验证身份";

            password += "@alice";
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(password));
            string result = BitConverter.ToString(hash);
            if (result == PasswordHash) {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1, "admin", DateTime.Now, DateTime.MaxValue, false, String.Empty);
                HttpCookie cookie = new HttpCookie(
                    FormsAuthentication.FormsCookieName,
                    FormsAuthentication.Encrypt(ticket)
                );
                Response.Cookies.Add(cookie);

                return Redirect(Url.Content("~/console/"));
            }
            else {
                ViewBag.Fail = true;
                return View();
            }
        }

        [Authorize]
        public ActionResult Index() {
            ViewBag.Title = "博客列表";

            string dir = Server.MapPath("~/Content");
            IEnumerable<FileInfo> files = new DirectoryInfo(dir)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTime);
            return View(files);
        }

        [Authorize]
        public void Update(string name) {
            FullPost post = DbSession.Get<FullPost>(name);
            HashSet<string> storedTags = new HashSet<string>();
            if (post == null) {
                post = new FullPost();
            }
            else {
                storedTags.UnionWith(post.Tags);
            }
            UpdatePost(name, post);

            // 有以前的tag列表说明数据库里有这一篇，使用更新
            if (storedTags.Count > 0) {
                DbSession.Update(post);
            }
            else {
                DbSession.Save(post);
            }

            // 抽取tag的区别
            HashSet<string> currentTags = new HashSet<string>(post.Tags);
            UpdateTags(currentTags, storedTags);

            // 更新全文索引
            UpdateIndex(post);
        }

        private void UpdatePost(string name, FullPost post) {
            string filename = Server.MapPath("~/Content/" + name + ".md");
            string[] lines = System.IO.File.ReadAllLines(filename);
            post.Name = name;
            post.Title = lines[0].Split(':')[1].Trim();
            post.PostDate = DateTime.ParseExact(lines[2].Split(':')[1].Trim(), "yyyy-MM-dd", null);
            post.UpdateDate = DateTime.Today;
            post.Tags = lines[1].Split(':')[1].Split(',').Select(t => t.Trim()).ToArray();

            StringBuilder excerpt = new StringBuilder();
            StringBuilder content = new StringBuilder();
            bool isExcerptRecorded = false;
            for (int i = 3; i < lines.Length; i++) {
                string line = lines[i];
                if (line.Trim() == "<!-- more -->") {
                    isExcerptRecorded = true;
                }

                content.AppendLine(line);
                if (!isExcerptRecorded) {
                    excerpt.AppendLine(line);
                }
            }
            post.Excerpt = excerpt.ToString().Trim();
            post.Content = content.ToString().Trim();
        }

        private void UpdateTags(HashSet<string> currentTags, HashSet<string> storedTags) {
            // 数据库里的减去现在的，就是被删除的tag
            foreach (string tagName in storedTags.Except(currentTags)) {
                // 被删除的tag在数据库里肯定已经有这一行，只要数量减1就行
                Tag tag = DbSession.Get<Tag>(tagName);
                tag.HitCount--;
                if (tag.HitCount == 0) {
                    DbSession.Delete(tag);
                }
                else {
                    DbSession.Update(tag);
                }
            }

            // 现在的减去数据库里的，就是新添加的tag
            foreach (string tagName in currentTags.Except(storedTags)) {
                // 新增的则要确认数据库里是否有
                Tag tag = DbSession.Get<Tag>(tagName);
                if (tag == null) {
                    tag = new Tag() {
                        Name = tagName,
                        HitCount = 1
                    };
                    DbSession.Save(tag);
                }
                else {
                    tag.HitCount++;
                    DbSession.Update(tag);
                }
            }
        }

        private void UpdateIndex(FullPost post) {
            throw new NotImplementedException();
        }
    }
}

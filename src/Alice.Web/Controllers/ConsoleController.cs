using Alice.Model;
using MarkdownDeep;
using NHibernate;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Alice.Web.Controllers {
    public class ConsoleController : Controller {
        [Inject]
        public ISession DbSession { get; set; }

        public ActionResult Index() {
            string dir = Server.MapPath("~/Content");
            IEnumerable<FileInfo> files = new DirectoryInfo(dir)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTime);
            return View(files);
        }

        public void Update(string name) {
            string filename = Server.MapPath("~/Content/" + name + ".md");
            string[] lines = System.IO.File.ReadAllLines(filename);

            FullPost post = DbSession.Get<FullPost>(name);
            HashSet<string> storedTags = new HashSet<string>();
            if (post == null) {
                post = new FullPost();
            }
            else {
                storedTags.UnionWith(post.Tags);
            }
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

            // 有以前的tag列表说明数据库里有这一篇，使用更新
            if (storedTags.Count > 0) {
                DbSession.Update(post);
            }
            else {
                DbSession.Save(post);
            }

            // 抽取tag的区别
            HashSet<string> currentTags = new HashSet<string>(post.Tags);
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
    }
}

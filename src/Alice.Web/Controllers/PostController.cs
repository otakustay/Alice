﻿using System;
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

namespace Alice.Web.Controllers {
    public class PostController : Controller {
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
            if (!Request.AcceptTypes.Contains("application/json")) {
                return Redirect(Url.Content("~/" + postName + "/"));
            }

            IEnumerable<Comment> comments = DbSession.QueryOver<Comment>()
                .Where(c => c.PostName == postName)
                .OrderBy(c => c.PostTime).Asc
                .List();

            return new NewtonJsonActionResult(comments);
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
                if (Request.AcceptTypes.Contains("application/json")) {
                    var result = new {
                        Success = false,
                        Errors = ModelState
                            .Where(m => m.Value.Errors.Any())
                            .ToDictionary(m => m.Key, m => m.Value.Errors[0].ErrorMessage)
                    };
                    return new NewtonJsonActionResult(result);
                }
                else {
                    PostEntry entry = DbSession.QueryOver<PostEntry>()
                        .Where(p => p.Name == comment.PostName)
                        .SingleOrDefault();
                    entry = RenderEntry(entry);
                    ViewBag.Comment = comment;
                    return View("ViewPost", entry);
                }
            }

            comment.PostTime = DateTime.Now;
            comment.Author.IpAddress = Request.UserHostAddress;
            comment.Author.UserAgent = Request.UserAgent;

            DbSession.Save(comment);

            if (Request.AcceptTypes.Contains("application/json")) {
                var result = new {
                    Success = true,
                    Comment = comment
                };
                return new CreatedActionResult(
                    Url.Content("~/" + comment.PostName),
                    new NewtonJsonActionResult(result)
                );
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
    }
}

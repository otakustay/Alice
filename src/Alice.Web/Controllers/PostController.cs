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

        [HttpGet]
        public ActionResult List(int page = 1) {
            if (page <= 0) {
                page = 1;
            }
            int start = (page - 1) * PageSize;

            using (MySqlConnection connection = new MySqlConnection(ConnectionString)) {
                connection.Open();

                MySqlCommand commandForCount = connection.CreateCommand();
                commandForCount.CommandType = CommandType.Text;
                commandForCount.CommandText = "select count(*) from post";
                int count = (int)((long)commandForCount.ExecuteScalar());

                MySqlCommand commandForPage = connection.CreateCommand();
                commandForPage.CommandType = CommandType.Text;
                commandForPage.CommandText = "select name, title, post_date, excerpt from post order by post_date desc limit ?start, ?limit";
                commandForPage.Parameters.AddWithValue("?start", start);
                commandForPage.Parameters.AddWithValue("?limit", PageSize);
                List<PostExcerpt> excerpts = new List<PostExcerpt>();
                using (IDataReader reader = commandForPage.ExecuteReader()) {
                    while (reader.Read()) {
                        PostExcerpt excerpt = new PostExcerpt() {
                            Name = reader["name"].ToString(),
                            Title = reader["title"].ToString(),
                            Excerpt = Transformer.Transform(reader["excerpt"].ToString().Trim()),
                            PostDate = (DateTime)reader["post_date"]
                        };
                        excerpts.Add(excerpt);
                    }
                }

                ViewBag.Title = "宅居 - 宅并技术着";
                ViewBag.PageCount = (int)Math.Ceiling((double)count / (double)PageSize);
                ViewBag.PageIndex = page;
                return View(excerpts);
            }
        }

        [HttpGet]
        public ActionResult ViewPost(string name) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select name, title, post_date, update_date, content, tags from post where name = ?name";
                command.Parameters.AddWithValue("?name", name);
                using (IDataReader reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        PostEntry entry = new PostEntry() {
                            Name = reader["name"].ToString(),
                            Title = reader["title"].ToString(),
                            Content = Transformer.Transform(reader["content"].ToString().Trim()),
                            Tags = reader["tags"].ToString().Split(','),
                            PostDate = (DateTime)reader["post_date"],
                            UpdateDate = (DateTime)reader["update_date"]
                        };
                        ViewBag.Title = String.Format("{0} - {1}", entry.Title, "宅居 - 宅并技术着");
                        return View(entry);
                    }
                    else {
                        return new HttpStatusCodeResult(404);
                    }
                }
            }
        }

        [ActionName("Comments")]
        [HttpGet]
        public ActionResult GetComments(string postName) {
            List<Comment> comments = new List<Comment>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from comment where post_name = ?postName order by post_time asc";
                command.Parameters.AddWithValue("?postName", postName);
                using (IDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        Comment comment = new Comment() {
                            Id = (int)reader["id"],
                            PostName = reader["post_name"].ToString(),
                            PostTime = (DateTime)reader["post_time"],
                            Content = reader["content"].ToString(),
                            Author = new CommentAuthor() {
                                Name = reader["author_name"].ToString(),
                                Email = reader["author_email"].ToString(),
                                IpAddress = reader["author_ip_address"].ToString(),
                                UserAgent = reader["author_user_agent"].ToString()
                            }
                        };
                        comments.Add(comment);
                    }
                }
            }

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

            using (MySqlConnection connection = new MySqlConnection(ConnectionString)) {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText =
@"insert into comment (
    post_name, content, post_time, author_name, author_email, author_ip_address, author_user_agent
) 
values (
    ?postName, ?content, ?postTime, ?authorName, ?authorEmail, ?authorIpAddress, ?authorUserAgent
)";
                command.Parameters.AddWithValue("?postName", comment.PostName);
                command.Parameters.AddWithValue("?content", comment.Content);
                command.Parameters.AddWithValue("?postTime", comment.PostTime);
                command.Parameters.AddWithValue("?authorName", comment.Author.Name);
                command.Parameters.AddWithValue("?authorEmail", comment.Author.Email);
                command.Parameters.AddWithValue("?authorIpAddress", comment.Author.IpAddress);
                command.Parameters.AddWithValue("?authorUserAgent", comment.Author.UserAgent);
                command.ExecuteNonQuery();

                if (Request.Headers["Accept"].Contains("application/json")) {
                    return new CreatedActionResult(Url.Content("~/" + comment.PostName));
                }
                else {
                    return Redirect(Url.Content("~/" + comment.PostName));
                }
            }
        }

        [HttpGet]
        public ActionResult Feed() {
            List<PostEntry> entries = new List<PostEntry>();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString)) {
                connection.Open();

                MySqlCommand commandForPage = connection.CreateCommand();
                commandForPage.CommandType = CommandType.Text;
                commandForPage.CommandText = "select name, title, post_date, update_date, content, tags from post order by post_date desc limit " + PageSize;
                using (IDataReader reader = commandForPage.ExecuteReader()) {
                    while (reader.Read()) {
                        PostEntry entry = new PostEntry() {
                            Name = reader["name"].ToString(),
                            Title = reader["title"].ToString(),
                            Content = Transformer.Transform(reader["content"].ToString().Trim()),
                            Tags = reader["tags"].ToString().Split(','),
                            PostDate = (DateTime)reader["post_date"],
                            UpdateDate = (DateTime)reader["update_date"]
                        };
                        entries.Add(entry);
                    }
                }
            }

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
    }
}

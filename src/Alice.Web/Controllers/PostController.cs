using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Alice.Model;
using MarkdownDeep;
using MySql.Data.MySqlClient;

namespace Alice.Web.Controllers {
    public class PostController : Controller {
        private static readonly Markdown markdown;

        static PostController() {
            markdown = new Markdown();
            markdown.ExtraMode = true;
            markdown.NewWindowForExternalLinks = true;
        }

        [HttpGet]
        public ActionResult List(int page = 1) {
            if (page <= 0) {
                page = 1;
            }
            int limit = 10;
            int start = (page - 1)* limit + 1;

            using (MySqlConnection connection = new MySqlConnection(MvcApplication.connectionString)) {
                connection.Open();

                MySqlCommand commandForCount = connection.CreateCommand();
                commandForCount.CommandType = CommandType.Text;
                commandForCount.CommandText = "select count(*) from post";
                int count = (int)((long)commandForCount.ExecuteScalar());

                MySqlCommand commandForPage = connection.CreateCommand();
                commandForPage.CommandType = CommandType.Text;
                commandForPage.CommandText = "select name, title, post_date, excerpt from post order by post_date desc limit ?start, ?limit";
                commandForPage.Parameters.AddWithValue("?start", start);
                commandForPage.Parameters.AddWithValue("?limit", limit);
                List<PostExcerpt> excerpts = new List<PostExcerpt>();
                using (IDataReader reader = commandForPage.ExecuteReader()) {
                    while (reader.Read()) {
                        PostExcerpt excerpt = new PostExcerpt() {
                            Name = reader["name"].ToString(),
                            Title = reader["title"].ToString(),
                            Excerpt = markdown.Transform(reader["excerpt"].ToString().Trim()),
                            PostDate = (DateTime)reader["post_date"]
                        };
                        excerpts.Add(excerpt);
                    }
                }

                ViewBag.PageCount = (int)Math.Ceiling((double)count / (double)limit);
                ViewBag.PageIndex = page;
                return View(excerpts);
            }
        }

        [HttpGet]
        public ActionResult ViewPost(string name) {
            using (MySqlConnection connection = new MySqlConnection(MvcApplication.connectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select name, title, post_date, content, tags from post where name = ?name";
                command.Parameters.AddWithValue("?name", name);
                using (IDataReader reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        PostEntry entry = new PostEntry();
                        entry.Name = reader["name"].ToString();
                        entry.Title = reader["title"].ToString();
                        entry.Content = markdown.Transform(reader["content"].ToString().Trim());
                        entry.Tags = reader["tags"].ToString().Split(',');
                        entry.PostDate = (DateTime)reader["post_date"];
                        return View(entry);
                    }
                    else {
                        return new HttpStatusCodeResult(404);
                    }
                }
            }
        }

    }
}

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
        }

        //
        // GET: /Home/

        [HttpGet]
        public ActionResult List(int page = 1) {
            if (page <= 0) {
                page = 1;
            }
            int limit = 10;
            int start = page * limit + 1;

            using (MySqlConnection connection = new MySqlConnection(MvcApplication.connectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from post order by post_date desc limit ?start, ?limit";
                command.Parameters.AddWithValue("?start", start);
                command.Parameters.AddWithValue("?limit", limit);
                List<Post> posts = new List<Post>();
                using (IDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        Post post = new Post();
                        post.Name = reader["name"].ToString();
                        post.Title = reader["title"].ToString();
                        post.Content = markdown.Transform(reader["content"].ToString().Trim());
                        post.Excerpt = markdown.Transform(reader["excerpt"].ToString().Trim());
                        post.Tags = reader["tags"].ToString().Split(',');
                        post.PostDate = (DateTime)reader["post_date"];
                        posts.Add(post);
                    }
                }
                return View(posts);
            }
        }

        [HttpGet]
        public ActionResult ViewPost(string name) {
            using (MySqlConnection connection = new MySqlConnection(MvcApplication.connectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from post where name = ?name";
                command.Parameters.AddWithValue("?name", name);
                using (IDataReader reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        Post post = new Post();
                        post.Name = reader["name"].ToString();
                        post.Title = reader["title"].ToString();
                        post.Content = markdown.Transform(reader["content"].ToString().Trim());
                        post.Excerpt = markdown.Transform(reader["excerpt"].ToString().Trim());
                        post.Tags = reader["tags"].ToString().Split(',');
                        post.PostDate = (DateTime)reader["post_date"];
                        return View(post);
                    }
                    else {
                        return new HttpStatusCodeResult(404);
                    }
                }
            }
        }

    }
}

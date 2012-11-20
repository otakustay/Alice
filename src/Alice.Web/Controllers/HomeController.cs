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
    public class HomeController : Controller {
        private static readonly Markdown markdown;

        static HomeController() {
            markdown = new Markdown();
            markdown.ExtraMode = true;
        }

        //
        // GET: /Home/

        [HttpGet]
        public ActionResult Index(int page) {
            if (page <= 0) {
                page = 1;
            }
            int limit = 10;
            int start = page * limit + 1;

            using (MySqlConnection connection = new MySqlConnection(MvcApplication.connectionString)) {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from Post order by PostDate desc limit ?start, ?limit";
                command.Parameters.AddWithValue("?start", start);
                command.Parameters.AddWithValue("?limit", limit);
                List<Post> posts = new List<Post>();
                using (IDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        Post post = new Post();
                        post.Name = reader["Name"].ToString();
                        post.Title = reader["Title"].ToString();
                        post.Content = markdown.Transform(reader["Content"].ToString());
                        post.Excerpt = markdown.Transform(reader["Excerpt"].ToString());
                        post.Tags = reader["Tags"].ToString().Split(',');
                        post.PostDate = (DateTime)reader["PostDate"];
                        posts.Add(post);
                    }
                }
                return View(posts);
            }
        }

    }
}

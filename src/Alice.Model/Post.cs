using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class Post {
        public string Name { get; set; }

        public string Title { get; set; }

        public string Excerpt { get; set; }

        public string Content { get; set; }

        public string[] Tags { get; set; }

        public DateTime PostDate { get; set; }
    }
}

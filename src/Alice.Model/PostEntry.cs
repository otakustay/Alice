using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class PostEntry : Post {
        public string Content { get; set; }

        public string[] Tags { get; set; }
    }
}

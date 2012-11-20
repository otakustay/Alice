using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public abstract class Post {
        public string Name { get; set; }

        public string Title { get; set; }

        public DateTime PostDate { get; set; }
    }
}

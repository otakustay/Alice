﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class FullPost : Post {
        public string Content { get; set; }

        public string[] Tags { get; set; }

        public DateTime UpdateDate { get; set; }

        public string Excerpt { get; set; }
    }
}
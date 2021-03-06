﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class Comment {
        public int Id { get; set; }

        public int? Target { get; set; }

        public string PostName { get; set; }

        public string Content { get; set; }

        public string Referrer { get; set; }

        public DateTime PostTime { get; set; }

        public CommentAuthor Author { get; set; }

        public bool Audited { get; set; }
    }
}

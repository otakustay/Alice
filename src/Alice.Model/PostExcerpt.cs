﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class PostExcerpt : Post {
        public string Excerpt { get; set; }

        public PostExcerpt Clone() {
            return new PostExcerpt() {
                Name = Name,
                Title = Title,
                PostDate = PostDate,
                Excerpt = Excerpt
            };
        }
    }
}

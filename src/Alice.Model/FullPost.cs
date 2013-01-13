using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class FullPost : Post {
        public string Content { get; set; }

        public string[] Tags { get; set; }

        public DateTime UpdateDate { get; set; }

        public string Excerpt { get; set; }

        public FullPost Clone() {
            return new FullPost() {
                Name = Name,
                Title = Title,
                PostDate = PostDate,
                Content = Content,
                Tags = (string[])Tags.Clone(),
                UpdateDate = UpdateDate,
                Excerpt = Excerpt
            };
        }
    }
}

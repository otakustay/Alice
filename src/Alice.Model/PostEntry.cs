using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alice.Model {
    public class PostEntry : Post {
        public string Content { get; set; }

        public string[] Tags { get; set; }

        public DateTime UpdateDate { get; set; }

        public PostEntry Clone() {
            return new PostEntry() {
                Name = Name,
                Title = Title,
                PostDate = PostDate,
                Content = Content,
                Tags = (string[])Tags.Clone(),
                UpdateDate = UpdateDate
            };
        }
    }
}

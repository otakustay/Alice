using Alice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alice.Web.Models {
    public class CommentAuditionView : CommentView {
        public PostExcerpt Post { get; set; }
    }
}
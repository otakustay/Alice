using Alice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alice.Web.Models {
    public class CommentView : Comment {
        public string TargetAuthorName { get; set; }
    }
}
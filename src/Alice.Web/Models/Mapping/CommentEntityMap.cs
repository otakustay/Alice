using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alice.Model;
using FluentNHibernate.Mapping;

namespace Alice.Web.Models.Mapping {
    public class CommentEntityMap : ClassMap<Comment>{
        public CommentEntityMap() {
            Id(c => c.Id).Column("id");
            Map(c => c.Content).Column("content");
            Map(c => c.PostName).Column("post_name");
            Map(c => c.PostTime).Column("post_time");

            Component(c => c.Author).ColumnPrefix("author_");

            Not.LazyLoad();
        }
    }
}
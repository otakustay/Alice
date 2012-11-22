using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alice.Model;
using FluentNHibernate.Mapping;

namespace Alice.Web.Models.Mapping {
    public class PostExcerptEntityMap : ClassMap<PostExcerpt> {
        public PostExcerptEntityMap() {
            Table("post");

            Id(p => p.Name);
            Map(p => p.PostDate).Column("post_date");
            Map(p => p.Title).Column("title");
            Map(p => p.Excerpt).Column("excerpt");

            Not.LazyLoad();
        }
    }
}
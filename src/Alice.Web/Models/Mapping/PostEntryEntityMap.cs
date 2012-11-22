using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alice.Model;
using FluentNHibernate.Mapping;

namespace Alice.Web.Models.Mapping {
    public class PostEntryEntityMap : ClassMap<PostEntry> {
        public PostEntryEntityMap() {
            Id(p => p.Name);
            Map(p => p.PostDate).Column("post_date");
            Map(p => p.Title).Column("post_title");
            Map(p => p.Content).Column("content");
            Map(p => p.Tags).Column("tags").CustomType<StringArrayType>();
            Map(p => p.UpdateDate).Column("update_date");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alice.Model;
using FluentNHibernate.Mapping;

namespace Alice.Web.Models.Mapping {
    public class CommentAuthorComponentMap : ComponentMap<CommentAuthor> {
        public CommentAuthorComponentMap() {
            Map(a => a.Email).Column("email");
            Map(a => a.IpAddress).Column("ip_address");
            Map(a => a.Name).Column("name");
            Map(a => a.UserAgent).Column("user_agent");
        }
    }
}
create table `post` (
  `name` varchar(200) not null,
  `title` varchar(200) not null,
  `content` text not null,
  `excerpt` text not null,
  `tags` varchar(400) not null,
  `post_date` datetime not null,
  PRIMARY KEY (`name`)
) engine=InnoDB default charset=utf8;
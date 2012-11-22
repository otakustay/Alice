create table `comment` (
  `id` int not null auto_increment,
  `post_name` varchar(200) not null,
  `author_name` varchar(120) not null,
  `author_email` varchar(200) not null,
  `author_ip_address` varchar(40) not null,
  `author_user_agent` varchar(200) not null,
  `content` text not null,
  `post_date` datetime not null,
  PRIMARY KEY (`id`)
) engine=InnoDB default charset=utf8;
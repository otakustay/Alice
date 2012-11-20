create table `tag` (
  `name` varchar(200) not null,
  `hit_count` int not null,
  PRIMARY KEY (`name`)
) engine=InnoDB default charset=utf8;
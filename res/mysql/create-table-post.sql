create table `Post` (
  `Name` varchar(200) not null,
  `Title` varchar(200) not null,
  `Content` text not null,
  `Excerpt` text not null,
  `Tags` varchar(400) not null,
  `PostDate` datetime not null,
  PRIMARY KEY (`Name`)
) engine=InnoDB default charset=utf8;
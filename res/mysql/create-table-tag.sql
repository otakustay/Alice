create table `Tag` (
  `Name` varchar(200) not null,
  `HitCount` int not null,
  PRIMARY KEY (`Name`)
) engine=InnoDB default charset=utf8;
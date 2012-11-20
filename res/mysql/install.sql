/* 
 * create user PingApp identified by 'password'
 * mysql -uPingApp -p
 */

create database `Alice` default character set utf8;

use `Alice`;

source create-table-post.sql;
source create-table-tag.sql;

show tables;
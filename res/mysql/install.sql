/* 
 * create user PingApp identified by 'password'
 * mysql -uPingApp -p
 */

create database `alice` default character set utf8;

use `alice`;

source create-table-post.sql;
source create-table-tag.sql;

show tables;
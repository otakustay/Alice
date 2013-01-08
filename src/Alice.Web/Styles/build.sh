#!/bin/sh
path=$(dirname $(echo ${0} | sed 's/\\/\//g'))
for theme in 'night' 'day'
do
    echo '@import "theme-'${theme}'.less";' > ${path}/theme.less
    for file in 'site' 'blog' 'list' 'post' 'console'
    do
        lessc ${path}/${file}'.less' ${path}/${file}'-'${theme}'.css'
    done
done
# exit 0
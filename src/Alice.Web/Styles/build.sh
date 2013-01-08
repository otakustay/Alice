for theme in 'day' 'night'
do
    echo '@import "theme-'${theme}'.less";' > theme.less
    for file in 'site' 'blog' 'list' 'post' 'console'
    do
        lessc ${file}'.less' ${file}'-'${theme}'.css'
    done
done
rm *.less
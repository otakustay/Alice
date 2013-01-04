(function() {
    if (!history || !history.pushState) {
        return;
    }
    // webkit在onload之后会触发一次popstate事件
    // 导致第一次进入时有一次无意义的ajax请求加载当前页面
    // 因此使用一个标记位去除这一次
    var firstHit = navigator.userAgent.indexOf('AppleWebKit') < 0;
    var styleSelector = 'head > link[rel="stylesheet"]:not([data-persist])';
    var scriptSelector = 'head > script:not([data-persist])';

    // 模拟浏览器，只能有一个页面加载，第2次点击时第1个加载必须中断
    var xhr;

    function updatePage(html) {
        var doc = document.createElement('html');
        doc.innerHTML = html;
        doc = $(doc);

        $(styleSelector).remove();
        doc.find(styleSelector).appendTo('head');

        $(scriptSelector).remove();
        doc.find(scriptSelector).appendTo('head');

        $('#main').hide().html(doc.find('#main').html()).fadeIn();

        loadHolmes();

        return doc;
    }

    function loadPage(url, pushHistoryState) {
        if (xhr) {
            xhr.abort();
        }

        $.get(
            url,
            function(html) {
                xhr = null;

                var doc = updatePage(html);

                if (pushHistoryState) {
                    history.pushState(html, doc.find('title').text(), url);
                }
            },
            'html'
        );
    }

    $('#page').on(
        'click',
        'a',
        function() {
            var href = $(this).attr('href');

            if (href.indexOf('/') !== 0) {
                return;
            }

            loadPage(href, true);

            return false;
        }
    );

    window.addEventListener(
        'popstate',
        function(e) {
            if (!firstHit) {
                firstHit = true;
                return;
            }

            var html = e.state;
            if (html) {
                updatePage(html);

                e.preventDefault();
            }
            else {
                loadPage(location.pathname, false);
            }
        }
    );
}());
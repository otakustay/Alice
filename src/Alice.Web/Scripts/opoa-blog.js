(function() {
    if (!history || !history.pushState) {
        return;
    }
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

    function loadPage(url) {
        if (xhr) {
            xhr.abort();
        }

        $.get(
            url,
            function(html) {
                xhr = null;

                var doc = updatePage(html);

                history.pushState(html, doc.find('title').text(), url);
            },
            'html'
        );
    }

    $('#page').on(
        'click',
        'a:not(#feed)',
        function() {
            var href = $(this).attr('href');

            if (href.indexOf('/') !== 0) {
                return;
            }

            loadPage(href);

            return false;
        }
    );

    $('#search > form')
        .off('submit')
        .on(
            'submit',
            function() {
                var keywords = $('#keywords').val().trim();
                if (keywords) {
                    var url = '/search/' + encodeURIComponent(keywords) + '/';
                    loadPage(url);
                }
                return false;
            }
        );

    // 由于进入下一个页面后再退回时，会触发popstate事件，且e.state为null导致后退无效
    // 因此在这一步需要先把history.state设置一下，以便后退可以生效
    function setInitialState() {
        var html = document.documentElement.outerHTML;
        history.replaceState(html, document.title);
    }
    $(document).ready(setInitialState);

    window.addEventListener(
        'popstate',
        function(e) {
            var html = e.state;
            if (html) {
                updatePage(html);

                e.preventDefault();
            }
        }
    );

    // TODO: 坑爹的是，如果点击了页面中的一个<a>导致hash的变化，再后退
    // 此时同样有popstate事件，同样有e.state
    // 且由于不能看后退前是什么URL，无法分辨是“来自另一页”还是“仅hash发生变化”
    // 因此只能原样执行popstate事件的处理函数，导致一个fadeIn的效果
    // 此问题暂无解决方案，History Interface的设计并不合理，应当提供relatedURL之类的属性标识来源
}());
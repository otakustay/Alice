(function() {
    var emailRule = /^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i;
    var postName = $('#post').data('name');
    var markdown = new MarkdownDeep.Markdown();
    markdown.SafeMode = true;
    markdown.NewWindowForExternalLinks = true;
    // 禁掉链接和href和图片的src上的伪协议，仅允许http和https协议
    (function() {
        var qualifyURL = MarkdownDeep.Markdown.prototype.OnQualifyUrl;

        MarkdownDeep.Markdown.prototype.OnQualifyUrl = function(url) {
            url = qualifyURL(url);
            // MarkdownDeep会转义attribute
            if (url.indexOf('http://') !== 0 && 
                url.indexOf('https://') !== 0 &&
                url.indexOf('#') !== 0) {
                return 'http://' + url;
            }
            return url;
        }
    }());

    function transformMarkdown(text, render) {
        return markdown.Transform(render(text));
    }

    function transformGravatar(text, render) {
        var email = render(text);
        var hash = md5(email.toLowerCase());
        return 'http://www.gravatar.com/avatar/' + hash + '?s=32&d=identicon&r=g';
    }

    function transformDate(text, render) {
        function pad(n) {
            n = n + '';
            return n.length === 1 ? '0' + n : n;
        }

        var date = render(text).slice(0, 16).replace('T', ' ');

        return date;
    }

    var authorMapping = {};

    function addAuthor(text, render) {
        var info = render(text).split('-');
        var id = info[0];
        var author = info.slice(1).join('-');
        authorMapping[id] = author;
        return '';
    }

    function getAuthorName(text, render) {
        var id = render(text);
        return authorMapping[id];
    }

    var transformers = {
        markdown: function() { return transformMarkdown; },
        gravatar: function() { return transformGravatar; },
        date: function() { return transformDate; },
        authorName: function() { return getAuthorName; },
        add: function() { return addAuthor; }
    };

    var articleTemplate = 
        '<li>' +
            '{{#add}}{{id}}-{{&author.name}}{{/add}}' +
            '<article id="comment-{{id}}">' +
                '<footer class="meta">' +
                    '<img class="author-avatar" src="{{#gravatar}}{{&author.email}}{{/gravatar}}" alt="{{author.name}}" />' +
                    '<span class="author-name">{{author.name}}</span>' +
                    '<time class="post-time" datetime="{{#date}}{{&postTime}}{{/date}}">' +
                        '{{#date}}{{&postTime}}{{/date}}' +
                    '</time>' +
                '</footer>' +
                '<section class="content">' +
                    '{{#target}}' +
                        '<p>回复 <a href="#comment-{{target}}" title="查看@{{#authorName}}{{target}}{{/authorName}}的发言">{{#authorName}}{{target}}{{/authorName}}</a></p>' +
                    '{{/target}}' +
                    '{{#markdown}}{{&content}}{{/markdown}}' +
                '</section>' +
                '<footer class="actions">' +
                    '<span title="回复{{author.name}}" class="reply">回复</span>' +
                '</footer>' +
            '</article>' + 
        '</li>';
    var sectionTemplate = 
        '<h1>已有<span>{{count}}</span>个评论</h1>' +
        '<ol>' + 
            '{{#comments}}' + 
            articleTemplate + 
            '{{/comments}}' +
        '</ol>';

    // 加载评论，需要md5库
    $.getScript(
        '/scripts/md5.min.js', 
        function() {
            $.getJSON(
                '/' + postName + '/comments',
                function(comments) {
                    var data = $.extend({ comments: comments, count: comments.length }, transformers);
                    var html = Mustache.render(sectionTemplate, data);
                    $('#comments').html(html);
                }
            );
        }
    );
    
    var postCommentForm = $('#post-comment > form');

    // 添加验证
    function validateForm() {
        var name = $('#name').val().trim();
        var email = $('#email').val().trim();
        var content = $('#content').val();
        var isValid = true;
        
        if (!name || name.length > 60) {
            isValid = false;
            $('#name ~ label').text('请正确填写昵称');
        }
        else {
            $('#name ~ label').empty();
        }

        if (email.length > 100 || !emailRule.test(email)) {
            isValid = false;
            $('#email ~ label').text('请正确填写邮箱');
        }
        else {
            $('#email ~ label').empty();
        }

        if (!content.trim()) {
            isValid = false;
            $('#content ~ label').text('请填写内容');
        }
        else {
            $('#content ~ label').empty();
        }

        return isValid;
    }

    // 提交评论
    postCommentForm.on(
        'submit',
        function() {
            if (!validateForm()) {
                return false;
            }

            $(':submit').attr('disabled', 'disabled');
            $.ajax({
                url: postCommentForm.attr('action'),
                type: 'post',
                data: $(this).serialize(),
                dataType: 'json',
                success: function(data) {
                    if (data.success) {
                        $('#comments > h1 > span').text(function() { return parseInt(this.innerHTML, 10) + 1; });
                        $.extend(data.comment, transformers);
                        var html = Mustache.render(articleTemplate, data.comment);
                        $('#comments > ol').append(html);
                        postCommentForm[0].reset();
                    }
                    else {
                        for (var name in data.errors) {
                            $('#' + name + ' ~ label').text(data.errors[name]);
                        }
                    }
                },
                complete: function() { $(':submit').removeAttr('disabled'); }
            });
            return false;
        }
    );

    // 回复评论
    function cancelReplyMode() {
        $('#post-comment > h1').text('留言评论');
        $('#cancel-reply').remove();
        $('input[name="comment.target"]').remove();
    }

    $('#comments').on(
        'click',
        '.reply',
        function() {
            cancelReplyMode();

            var container = $(this).closest('article');
            var authorName = container.find('.author-name').text();
            var id = container.attr('id').split('-')[1];

            $('#post-comment > h1')
                .text('回复 @' + authorName)
                .append('<span id="cancel-reply">退出回复模式</span>');
            postCommentForm.prepend('<input type="hidden" name="comment.target" value="' + id + '" />');

            var scrollTop = $('#post-comment').offset().top;
            var win = $(window);
            var padding = win.height() / 4;
            if (scrollTop < win.scrollTop() || scrollTop > win.scrollTop + win.height()) {
                $('html, body').animate({ scrollTop: scrollTop - padding });
            }
        }
    );
    $('#post-comment > h1').on('click', '#cancel-reply', cancelReplyMode);
}());
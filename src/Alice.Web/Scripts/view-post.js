(function() {
    var emailRule = /^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i;
    var postName = $('#post').data('name');
    var markdown = new MarkdownDeep.Markdown();
    markdown.SafeMode = true;
    markdown.NewWindowForExternalLinks = true;
    markdown.NewWindowForLocalLinks = true;

    function transformMarkdown(text, render) {
        return markdown.Transform(render(text));
    }

    var articleTemplate = 
        '<li>' +
            '<article>' +
                '<footer>' +
                    '<p class="author-name">{{author.name}}</p>' +
                    '<time class="post-time" datetime="{{postTime}}">{{prettyTime}}</time>' +
                '</footer>' +
                '{{#markdown}}{{content}}{{/markdown}}' +
            '</article>' + 
        '</li>';
    var sectionTemplate = 
        '<h1>已有<span>{{count}}</span>个评论</h1>' +
        '<ol>' + 
            '{{#comments}}' + 
            articleTemplate + 
            '{{/comments}}' +
        '</ol>';

    // 加载评论
    $.getJSON(
        '/' + postName + '/comments',
        function(comments) {
            var data = { 
                comments: comments, 
                count: comments.length, 
                markdown: function() { return transformMarkdown; }
            };
            var html = Mustache.render(sectionTemplate, data);
            $('#comments').html(html);
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

            $.post(
                '/' + postName + '/comments/',
                $(this).serialize(),
                function(data) {
                    if (data.success) {
                        $('#comments > h1 > span').text(function() { return parseInt(this.innerHTML, 10) + 1; });
                        data.comment.markdown = function() { return transformMarkdown; };
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
                'json'
            );
            return false;
        }
    );
}());
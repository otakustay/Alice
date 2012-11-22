var postName = $('#post').data('name');
var markdown = new MarkdownDeep.Markdown();
markdown.SafeMode = true;
markdown.NewWindowForExternalLinks = true;
markdown.NewWindowForLocalLinks = true;

function transformMarkdown(text, render) {
    return markdown.Transform(render(text));
}

// 加载评论
(function() {
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
    postCommentForm.on(
        'submit',
        function() {
            $.post(
                '/' + postName + '/comments',
                $(this).serialize(),
                function(data) {
                    $('#comments > h1 > span').text(function() { return parseInt(this.innerHTML, 10) + 1; });
                    debugger;
                    data.markdown = function() { return transformMarkdown; };
                    var html = Mustache.render(articleTemplate, data);
                    $('#comments > ol').append(html);
                    postCommentForm[0].reset();
                },
                'json'
            );
            return false;
        }
    );
}());
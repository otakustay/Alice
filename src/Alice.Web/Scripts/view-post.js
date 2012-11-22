var postName = $('#post').data('name');

// 加载评论
(function() {
    var articleTemplate = 
        '<li>' +
            '<article>' +
                '<footer>' +
                    '<p class="author-name">{{author.name}}</p>' +
                    '<time class="post-time" datetime="{{postTime}}">{{prettyTime}}</time>' +
                '</footer>' +
                '{{&content}}' +
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
        function(data) {
            var html = Mustache.render(sectionTemplate, { comments: data, count: data.length });
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
                    var html = Mustache.render(articleTemplate, data);
                    $('#comments > h1 > span').text(function() { return parseInt(this.innerHTML, 10) + 1; });
                    $('#comments > ol').append(html);
                    postCommentForm[0].reset();
                },
                'json'
            );
            return false;
        }
    );
}());
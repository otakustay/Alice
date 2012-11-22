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
    var listTemplate = '{{#comments}}' + articleTemplate + '{{/comments}}';

    $.getJSON(
        '/' + postName + '/comments',
        function(data) {
            var html = Mustache.render(listTemplate, { comments: data });
            $('#comments').html(html);
        }
    );

    var postCommentForm = $('#post-comment > form');
    postCommentForm.on(
        'submit',
        function() {
            var data = {
                author: {
                    name: $('[name="comment.author.name"]').val(),
                    email: $('[name="comment.author.email"]').val()
                },
                content: $('[name="comment.content"]').val()
            };
            $.post(
                '/' + postName + '/comments',
                $(this).serialize(),
                function() {
                    var html = Mustache.render(articleTemplate, data);
                    $('#comments').append(html);
                    postCommentForm[0].reset();
                },
                'json'
            );
            return false;
        }
    );
}());
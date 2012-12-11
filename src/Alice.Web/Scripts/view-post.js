(function() {
    var emailRule = /^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i;
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
                success: function(data) {
                    // 成功时返回HTML片段，失败时返回错误集合
                    if (typeof data === 'string') {
                        $('#comments > h1 > span').text(function() { return parseInt(this.innerHTML, 10) + 1; });
                        $('#comments > ol').append(data);
                        cancelReplyMode();
                        postCommentForm[0].reset();
                    }
                    else {
                        for (var name in data) {
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
        return false;
    }

    $('#comments').on(
        'click',
        '.reply',
        function() {
            debugger;
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
            if (scrollTop < win.scrollTop() || scrollTop > win.scrollTop() + win.height()) {
                $('html, body').stop(true).animate({ scrollTop: scrollTop - padding });
            }

            return false;
        }
    );
    $('#post-comment > h1').on('click', '#cancel-reply', cancelReplyMode);
}());
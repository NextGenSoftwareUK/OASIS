$(document).ready(function () {
    $("form").submit(function (event) {
        var formData = {
            email: $("#email").val(),
            password: $("#password").val(),
        };

        if (!data.success) {
            if (data.errors.email) {
                $("#name-group").addClass("has-error");
                $("#name-group").append(
                    '<span class=="cd-signin-modal__error">' + data.errors.email + "</span>"
                );
            }

            if (data.errors.password) {
                $("#email-group").addClass("has-error");
                $("#email-group").append(
                    '<span class=="cd-signin-modal__error">' + data.errors.password + "</span>"
                );
            }

        } else {
            $("form").html(
                '<div class="alert alert-success">' + data.message + "</div>"
            );
        }
        
        $.ajax({
            type: "POST",
            url: "https://api.oasisplatform.world/api/avatar/authenticate/",
            data: formData,
            dataType: "json",
            encode: true,
        }).done(function (data) {
            console.log(data);
        })
    })
})


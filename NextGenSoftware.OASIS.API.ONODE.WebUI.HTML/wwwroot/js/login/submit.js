// function checkPasswordMatch() {
//     var password = $("#signup-password").val();
//     var confirmPassword = $("#signup-password-confirm").val();

//     if (password != confirmPassword)
//         $("input[id=signup-password-confirm]").addClass("cd-signin-modal__input--has-error");
//     else
//         ;
// }

// $(document).ready(function () {
//     $("#signup-password, #signup-password-confirm").keyup(checkPasswordMatch);
// });

$(document).ready(function () {
    $("#submit_login").click(function (event) {
        var formData = {
            email: $("#email").val(),
            password: $("#password").val(),
        };

        $.ajax({
            type: "POST",
            url: "https://api.oasisplatform.world/api/avatar/authenticate/",
            data: formData,
            dataType: "json",
            encode: true,
        }).done(function (data) {
            console.log(data);
        });

        if (!data.success) {
            if (data.errors.email) {
                $("#email-group").addClass("has-error");
                $("#email-group").append(
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


        event.preventDefault();
    })
})
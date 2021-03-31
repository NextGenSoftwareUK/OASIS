$(document).ready(function () {
	
	
	$("#login-submit").click(function () {
		// e.preventDefault();
		// var validate = Validate();
		// var token = localStorage.getItem("AccessToken");
		// var password = $("#login-password").val();
		// var email = $("#login-email").val();
		// var confirmPassword = $("#signup-password-confirm").val();
		// var form = $(this);
		// var url = form.attr('action');
		
		var settings = {
			url: "https: //api.oasisplatform.world/api/avatar/authenticate/",
			methos: "POST",
			timeout: 0,
			headers: {
				"Content-type": "application/json",
				"Cookie": ""
			},
			data: JSON.stringify({
				"email": $("#login-email").val(),
				"password": $("#login-password").val()
			}),
		};
		$.ajax(settings).done(function (response) {
			console.log(response);
		});
    });
});

function checkPasswordMatch() {
	if (password != confirmPassword)
	$("input[id=signup-password-confirm]").addClass("cd-signin-modal__input--has-error");
};

$("#signup-password, #signup-password-confirm").keyup(checkPasswordMatch);
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


	var submit = document.getElementById('submit-login')
	//check for empty
	function validate(event) {
	    event.preventDefault();
	    var email = document.getElementById('signin-email');
	    var password = document.getElementById('signin-password');

	    if (email.value === "") {
	        Swal.fire({
	            title: 'Email Blank',
	            text: 'Please enter your email address.',
	            icon: 'error',
	            confirmButtonText: 'Ok'
	        });
	        email.focus();
	        return false;
	    }

	    if (!emailIsValid(email.value)) {
	        Swal.fire({
	            title: 'Email Invalid',
	            text: 'Please enter a valid email address.',
	            icon: 'error',
	            confirmButtonText: 'OK'
	        })
	        email.focus();
	        return false;
	    }

	    if (password.value === "") {
	        Swal.fire({
	            title: 'Password Blank',
	            text: 'Please enter your password.',
	            icon: 'error',
	            confirmButtonText: 'Ok'
	        });
	        password.focus();
	        return false;
	    }

	    return true;
	}

	emailIsValid = email => {
	    return /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/.test(email);
	}

submit.addEventListener('click', validate);
	
var xhr = new XMLHttpRequest();

xhr.open('POST', 'https://api.oasisplatform.world/api/avatar/authenticate/', true);
xhr.setRequestHeader("Content-type", "application/json");
xhr.send();

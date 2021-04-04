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
			method: "POST",
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


// To add to form validation

const usernameEl = document.querySelector('#username');
const emailEl = document.querySelector('#email');
const passwordEl = document.querySelector('#password');
const confirmPasswordEl = document.querySelector('#confirm-password');

const form = document.querySelector('#signup');


const checkUsername = () => {

	let valid = false;

	const min = 3,
		max = 25;

	const username = usernameEl.value.trim();

	if (!isRequired(username)) {
		showError(usernameEl, 'Username cannot be blank.');
	} else if (!isBetween(username.length, min, max)) {
		showError(usernameEl, `Username must be between ${min} and ${max} characters.`)
	} else {
		showSuccess(usernameEl);
		valid = true;
	}
	return valid;
};


const checkEmail = () => {
	let valid = false;
	const email = emailEl.value.trim();
	if (!isRequired(email)) {
		showError(emailEl, 'Email cannot be blank.');
	} else if (!isEmailValid(email)) {
		showError(emailEl, 'Email is not valid.')
	} else {
		showSuccess(emailEl);
		valid = true;
	}
	return valid;
};

const checkPassword = () => {
	let valid = false;


	const password = passwordEl.value.trim();

	if (!isRequired(password)) {
		showError(passwordEl, 'Password cannot be blank.');
	} else if (!isPasswordSecure(password)) {
		showError(passwordEl, 'Password must has at least 8 characters that include at least 1 lowercase character, 1 uppercase characters, 1 number, and 1 special character in (!@#$%^&*)');
	} else {
		showSuccess(passwordEl);
		valid = true;
	}

	return valid;
};

const checkConfirmPassword = () => {
	let valid = false;
	// check confirm password
	const confirmPassword = confirmPasswordEl.value.trim();
	const password = passwordEl.value.trim();

	if (!isRequired(confirmPassword)) {
		showError(confirmPasswordEl, 'Please enter the password again');
	} else if (password !== confirmPassword) {
		showError(confirmPasswordEl, 'The password does not match');
	} else {
		showSuccess(confirmPasswordEl);
		valid = true;
	}

	return valid;
};

const isEmailValid = (email) => {
	const re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
	return re.test(email);
};

const isPasswordSecure = (password) => {
	const re = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#\$%\^&\*])(?=.{8,})");
	return re.test(password);
};

const isRequired = value => value === '' ? false : true;
const isBetween = (length, min, max) => length < min || length > max ? false : true;


const showError = (input, message) => {
	// get the form-field element
	const formField = input.parentElement;
	// add the error class
	formField.classList.remove('success');
	formField.classList.add('error');

	// show the error message
	const error = formField.querySelector('small');
	error.textContent = message;
};

const showSuccess = (input) => {
	// get the form-field element
	const formField = input.parentElement;

	// remove the error class
	formField.classList.remove('error');
	formField.classList.add('success');

	// hide the error message
	const error = formField.querySelector('small');
	error.textContent = '';
}


form.addEventListener('submit', function (e) {
	// prevent the form from submitting
	e.preventDefault();

	// validate fields
	let isUsernameValid = checkUsername(),
		isEmailValid = checkEmail(),
		isPasswordValid = checkPassword(),
		isConfirmPasswordValid = checkConfirmPassword();

	let isFormValid = isUsernameValid &&
		isEmailValid &&
		isPasswordValid &&
		isConfirmPasswordValid;

	// submit to the server if the form is valid
	if (isFormValid) {

	}
});


const debounce = (fn, delay = 500) => {
	let timeoutId;
	return (...args) => {
		// cancel the previous timer
		if (timeoutId) {
			clearTimeout(timeoutId);
		}
		// setup a new timer
		timeoutId = setTimeout(() => {
			fn.apply(null, args)
		}, delay);
	};
};

form.addEventListener('input', debounce(function (e) {
	switch (e.target.id) {
		case 'username':
			checkUsername();
			break;
		case 'email':
			checkEmail();
			break;
		case 'password':
			checkPassword();
			break;
		case 'confirm-password':
			checkConfirmPassword();
			break;
	}
}));
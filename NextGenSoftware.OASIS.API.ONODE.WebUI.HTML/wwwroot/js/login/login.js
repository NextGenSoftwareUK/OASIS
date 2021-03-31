var loggedIn;

(function () {
	//Login/Signup modal window - by CodyHouse.co
	function ModalSignin(element) {
		this.element = element;
		this.blocks = this.element.getElementsByClassName('js-signin-modal-block');
		this.switchers = this.element.getElementsByClassName('js-signin-modal-switcher')[0].getElementsByTagName('a');
		this.triggers = document.getElementsByClassName('js-signin-modal-trigger');
		this.hidePassword = this.element.getElementsByClassName('js-hide-password');
		this.init();
	};

	ModalSignin.prototype.init = function () {
		//alert("ModalSignin.prototype.init");

		var self = this;
		//open modal/switch form
		for (var i = 0; i < this.triggers.length; i++) {
			//alert("trigger " + i);

			(function (i) {
				//alert("self = " + self);
				//alert("self.triggers[i] = " + self.triggers[i]);

				self.triggers[i].addEventListener('click', function (event) {
					//	alert("here");

					if (event.target.hasAttribute('data-signin')) {
						//	alert("has sign in");

						event.preventDefault();
						self.showSigninForm(event.target.getAttribute('data-signin'));
					}
				});
			})(i);
		}

		//close modal
		this.element.addEventListener('click', function (event) {
			//if( hasClass(event.target, 'js-close') ) {
			if (hasClass(event.target, 'js-signin-modal') || hasClass(event.target, 'js-close')) {
				// document.getElementById("divNewAccount").style.display = "none";
				// document.getElementById("divLogin").style.display = "block";
				// document.getElementById("divForgotPassword").style.display = "none";
				event.preventDefault();
				removeClass(self.element, 'cd-signin-modal--is-visible');
			}
		});
		//close modal when clicking the esc keyboard button
		document.addEventListener('keydown', function (event) {
			(event.which == '27') && removeClass(self.element, 'cd-signin-modal--is-visible');
		});

		//hide/show password
		for (var i = 0; i < this.hidePassword.length; i++) {
			(function (i) {
				self.hidePassword[i].addEventListener('click', function (event) {
					self.togglePassword(self.hidePassword[i]);
				});
			})(i);
		}
	};



	ModalSignin.prototype.togglePassword = function (target) {
		var password = target.previousElementSibling;
		('password' == password.getAttribute('type')) ? password.setAttribute('type', 'text') : password.setAttribute('type', 'password');
		target.textContent = ('Hide' == target.textContent) ? 'Show' : 'Hide';
		putCursorAtEnd(password);
	};

	ModalSignin.prototype.showSigninForm = function (type) 
	{
		//alert("show sign in form");
		//showLogin();

		// document.getElementById("divNewAccount").style.display = "none";
		// document.getElementById("divLogin").style.display = "none";
		// document.getElementById("divForgotPassword").style.display = "none";

		// show modal if not visible
		!hasClass(this.element, 'cd-signin-modal--is-visible') && addClass(this.element, 'cd-signin-modal--is-visible');
		// show selected form
		for (var i = 0; i < this.blocks.length; i++)
		{
			this.blocks[i].getAttribute('data-type') == type ? addClass(this.blocks[i], 'cd-signin-modal__block--is-selected') : removeClass(this.blocks[i], 'cd-signin-modal__block--is-selected');
		}
		//update switcher appearance
		var switcherType = (type == 'signup') ? 'signup' : 'login';
		for (var i = 0; i < this.switchers.length; i++)
		{
			this.switchers[i].getAttribute('data-type') == switcherType ? addClass(this.switchers[i], 'cd-selected') : removeClass(this.switchers[i], 'cd-selected');
		}
	};

	ModalSignin.prototype.toggleError = function (input, bool) {
		// used to show error messages in the form
		toggleClass(input, 'cd-signin-modal__input--has-error', bool);
		toggleClass(input.nextElementSibling, 'cd-signin-modal__error--is-visible', bool);
	};

	var signinModal = document.getElementsByClassName("js-signin-modal")[0];
	if (signinModal)
	{
		//console.log("here");
		new ModalSignin(signinModal);
	};

	// toggle main navigation on mobile
	var mainNav = document.getElementsByClassName('js-main-nav')[0];
	if (mainNav)
	{
		mainNav.addEventListener('click', function (event)
		{
			if (hasClass(event.target, 'js-main-nav'))
			{
				var navList = mainNav.getElementsByTagName('ul')[0];
				toggleClass(navList, 'cd-main-nav__list--is-visible', !hasClass(navList, 'cd-main-nav__list--is-visible'));
			};
		});
	};

	//class manipulations - needed if classList is not supported
	function hasClass(el, className)
	{
		if (el.classList) return el.classList.contains(className);
		else return !!el.className.match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
	};
	function addClass(el, className)
	{
		var classList = className.split(' ');
		if (el.classList) el.classList.add(classList[0]);
		else if (!hasClass(el, classList[0])) el.className += " " + classList[0];
		if (classList.length > 1) addClass(el, classList.slice(1).join(' '));
	};
	function removeClass(el, className)
	{
		var classList = className.split(' ');
		if (el.classList) el.classList.remove(classList[0]);
		else if (hasClass(el, classList[0]))
		{
			var reg = new RegExp('(\\s|^)' + classList[0] + '(\\s|$)');
			el.className = el.className.replace(reg, ' ');
		}
		if (classList.length > 1) removeClass(el, classList.slice(1).join(' '));
	};
	function toggleClass(el, className, bool)
	{
		if (bool) addClass(el, className);
		else removeClass(el, className);
	};

	//credits http://css-tricks.com/snippets/jquery/move-cursor-to-end-of-textarea-or-input/
	function putCursorAtEnd(el)
	{
		if (el.setSelectionRange)
		{
			var len = el.value.length * 2;
			el.focus();
			el.setSelectionRange(len, len);
		} else
		{
			el.value = el.value;
		}
	};
})();

	$("#login-submit").click(function (e) {
		e.preventDefault();
		// var validate = Validate();
		// var token = localStorage.getItem("AccessToken");
		// var password = $("#login-password").val();
		// var email = $("#login-email").val();
		// var confirmPassword = $("#signup-password-confirm").val();
		// var form = $(this);
		// var url = form.attr('action');

		var settings = {
			url: "https://api.oasisplatform.world/api/avatar/authenticate/",
			method: "POST",
			timeout: 0,
			headers: {
				"Content-type": "application/json",
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

var submit = document.getElementById('login-submit')
//check for empty
function validate(event) {
	event.preventDefault();
	var email = document.getElementById('login-email');
	var password = document.getElementById('login-password');

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

// submit.addEventListener('click', validate);

// function showResetPassword()
// {
// 	document.getElementById("divForgotPassword").style.display = "block";
// 	document.getElementById("divLogin").style.display = "none";
// 	document.getElementById("divNewAccount").style.display = "none";	
// };


// function showLogin()
// {
// 	console.log("SHOW LOGIN");
// 	console.log("b4 = " + document.getElementById("divLogin").style.display);

// 	document.getElementById("divLogin").style.display = "block";
// 	document.getElementById("divForgotPassword").style.display = "none";
// 	document.getElementById("divNewAccount").style.display = "none";	

// 	console.log("after = " + document.getElementById("divLogin").style.display);
// };

// function showNewAccount()
// {
// 	document.getElementById("divNewAccount").style.display = "block";
// 	document.getElementById("divLogin").style.display = "none";
// 	document.getElementById("divForgotPassword").style.display = "none";
// };

function createCookie(key, value, date) 
{;
	let expiration = new Date(date).toUTCString();
	let cookie = escape(key) + "=" + escape(value) + ";expires=" + expiration + ";";
	document.cookie = cookie;
	console.log(cookie);
	console.log("Creating new cookie with key: " + key + " value: " + value + " expiration: " + expiration);
};



function readCookie(name) 
{
	let key = name + "=";
	let cookies = document.cookie.split(';');

	console.log("cookies = " + cookies);

	for (let i = 0; i < cookies.length; i++) {
		let cookie = cookies[i];
		while (cookie.charAt(0) === ' ') {
			cookie = cookie.substring(1, cookie.length);
		}
		if (cookie.indexOf(key) === 0) {
			return cookie.substring(key.length, cookie.length);
		}
	}
	return null;
};


function createCORSRequest(method, url) 
{
	const xhr = new XMLHttpRequest();
	if ("withCredentials" in xhr) 
	{
		// Check if the XMLHttpRequest object has a "withCredentials" property.
		// "withCredentials" only exists on XMLHTTPRequest2 objects.
		console.log("CORS withCredentials");
		xhr.open(method, url, true);
	}
	else if (typeof XDomainRequest != "undefined") 
	{
		// Otherwise, check if XDomainRequest.
		// XDomainRequest only exists in IE, and is IE's way of making CORS requests.
		console.log("CORS IE");
		xhr = new XDomainRequest();
		xhr.open(method, url);
	}
	else 
	{
		// Otherwise, CORS is not supported by the browser.
		console.log("CORS NOT SUPPORTED");
		xhr = null;
	}

	return xhr;
};
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
				// $("#NewAccount").style.display = "none";
				// $("#Login").style.display = "block";
				// $("#ForgotPassword").style.display = "none";
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
		('password' == password.getAttribute('type')) ? password.setAttribute('type', 'text'): password.setAttribute('type', 'password');
		target.textContent = ('Hide' == target.textContent) ? 'Show' : 'Hide';
		putCursorAtEnd(password);
	};

	ModalSignin.prototype.showSigninForm = function (type) {
		//alert("show sign in form");
		//showLogin();

		// $("#NewAccount").style.display = "none";
		// $("#Login").style.display = "none";
		// $("#ForgotPassword").style.display = "none";

		// show modal if not visible
		!hasClass(this.element, 'cd-signin-modal--is-visible') && addClass(this.element, 'cd-signin-modal--is-visible');
		// show selected form
		for (var i = 0; i < this.blocks.length; i++) {
			this.blocks[i].getAttribute('data-type') == type ? addClass(this.blocks[i], 'cd-signin-modal__block--is-selected') : removeClass(this.blocks[i], 'cd-signin-modal__block--is-selected');
		}
		//update switcher appearance
		var switcherType = (type == 'signup') ? 'signup' : 'login';
		for (var i = 0; i < this.switchers.length; i++) {
			this.switchers[i].getAttribute('data-type') == switcherType ? addClass(this.switchers[i], 'cd-selected') : removeClass(this.switchers[i], 'cd-selected');
		}
	};

	ModalSignin.prototype.toggleError = function (input, bool) {
		// used to show error messages in the form
		toggleClass(input, 'cd-signin-modal__input--has-error', bool);
		toggleClass(input.nextElementSibling, 'cd-signin-modal__error--is-visible', bool);
	};

	var signinModal = document.getElementsByClassName("js-signin-modal")[0];
	if (signinModal) {
		//console.log("here");
		new ModalSignin(signinModal);
	};

	// toggle main navigation on mobile
	var mainNav = document.getElementsByClassName('js-main-nav')[0];
	if (mainNav) {
		mainNav.addEventListener('click', function (event) {
			if (hasClass(event.target, 'js-main-nav')) {
				var navList = mainNav.getElementsByTagName('ul')[0];
				toggleClass(navList, 'cd-main-nav__list--is-visible', !hasClass(navList, 'cd-main-nav__list--is-visible'));
			};
		});
	};

	//class manipulations - needed if classList is not supported
	function hasClass(el, className) {
		if (el.classList) return el.classList.contains(className);
		else return !!el.className.match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
	};

	function addClass(el, className) {
		var classList = className.split(' ');
		if (el.classList) el.classList.add(classList[0]);
		else if (!hasClass(el, classList[0])) el.className += " " + classList[0];
		if (classList.length > 1) addClass(el, classList.slice(1).join(' '));
	};

	function removeClass(el, className) {
		var classList = className.split(' ');
		if (el.classList) el.classList.remove(classList[0]);
		else if (hasClass(el, classList[0])) {
			var reg = new RegExp('(\\s|^)' + classList[0] + '(\\s|$)');
			el.className = el.className.replace(reg, ' ');
		}
		if (classList.length > 1) removeClass(el, classList.slice(1).join(' '));
	};

	function toggleClass(el, className, bool) {
		if (bool) addClass(el, className);
		else removeClass(el, className);
	};

	//credits http://css-tricks.com/snippets/jquery/move-cursor-to-end-of-textarea-or-input/
	function putCursorAtEnd(el) {
		if (el.setSelectionRange) {
			var len = el.value.length * 2;
			el.focus();
			el.setSelectionRange(len, len);
		} else {
			el.value = el.value;
		}
	};
})();

// Login

function login({
	jwt_token,
	jwt_token_expiry
}) {
	inMemoryToken = {
		token: jwt_token,
		expiry: jwt_token_expiry
	};
}

async function logout() {
	inMemoryToken = null;
	$.ajax({
		url: 'https://api.oasisplatform.world/api/avatar/revoke-token',
		method: "POST",
		headers: {
			"Authorization": "Bearer ${appJWTToken}",
			"Content-type": "application/json",
			"Cookie": "refresh-token="
		},
		data: JSON.stringify({
			"token": ""
		})
	})

	window.localStorage.setItem('logout', Date.now())
	location.href = "index.html"
}

function syncLogout() {
	if (event.key === 'logout') {
		console.log('logged out from storage!');
		location.href = "index.html"
	};
}

async function auth(ctx) {
	const {
		refresh_token
	} = nextCookie(ctx)

	/*
	 * If `ctx.req` is available it means we are on the server.
	 * Additionally if there's no token it means the user is not logged in.
	 */
	if (!inMemoryToken) {

		const headers = ctx && ctx.req ? {
			'Cookie': ctx.req.headers.cookie
		} : {}
		const hostname = typeof window === 'object' ? `${window.location.protocol}${window.location.host}` : `${ctx.req.headers.referer.split('://')[0]}://${ctx.req.headers.host}`
		const url = ctx && ctx.req ? `${hostname}/api/refresh-token` : '/api/refresh-token'
		console.log(url)
		try {
			const response = await fetch(url, {
				method: 'POST',
				credentials: 'include',
				headers: {
					'Content-Type': 'application/json',
					'Cache-Control': 'no-cache',
					...headers
				},
				body: JSON.stringify({})
			})
			if (response.status === 200) {
				const {
					jwt_token,
					refresh_token,
					jwt_token_expiry,
					refresh_token_expiry
				} = await response.json()
				// setup httpOnly cookie if SSR
				if (ctx && ctx.req) {
					ctx.res.setHeader('Set-Cookie', `refresh_token=${refresh_token};HttpOnly;Max-Age=${refresh_token_expiry};Path="/"`);
				}
				await login({
					jwt_token,
					jwt_token_expiry
				}, true)
			} else {
				let error = new Error(response.statusText)
				error.response = response
				throw error
			}
		} catch (error) {
			console.log(error)
			if (ctx && ctx.req) {
				ctx.res.writeHead(302, {
					Location: '/login'
				})
				ctx.res.end()
			}
			Router.push('/login')
		}
	}

	const jwt_token = inMemoryToken;

	// We already checked for server. This should only happen on client.
	if (!jwt_token) {
		Router.push('/login')
	}

	return jwt_token
}

function getToken() {
	return inMemoryToken
}


async function handleSubmit() {

	response = await fetch('https://api.oasisplatform.world/api/avatar/authenticate', {
		method: "POST",
		headers: {
			"Content-type": "application/json",
		},
		data: JSON.stringify({
			"email": $("#login-email").val(),
			"password": $("#login-password").val(),
		})
	})

	var {
		jwt_token
	} = await response.json()

}

// inMemoryToken;


jwt_token = inMemoryToken;
if (!jwt_token) {
	$('#"cd-signin-modal').modal('hide');
	removeClass(self.element, 'cd-signin-modal--is-visible');
}
return jwt_token

// await login({
// 	jwt_token
// })

// let appJWTToken
// const httpLink = new HttpLink({
// 	uri: 'https://graphql-jwt-tutorial.herokuapp.com/v1/graphql'
// })
// const authMiddleware = new ApolloLink((operation, forward) => {
// 	if (appJWTToken) {
// 		operation.setContext({
// 			headers: {
// 				Authorization: `Bearer ${appJWTToken}`
// 			}
// 		});
// 	}
// 	return forward(operation);
// })

// const apolloClient = new ApolloClient({
// 	link: concat(authMiddleware, httpLink),
// 	cache: new InMemoryCache(),
// });

$("#loginForm").submit(function (event) {
	event.preventDefault();

	$.ajax({
		url: "https://api.oasisplatform.world/api/avatar/authenticate/",
		method: "POST",
		headers: {
			"Content-type": "application/json",
		},
		data: JSON.stringify({
			"email": $("#login-email").val(),
			"password": $("#login-password").val(),
		}).done(function (data) {
			$.cookie('token', data.token)
		})
	});


	$(document).ajaxSuccess(function () {
		localStorage.setItem('access_token', token);
		console.log(response);
	});

	$(document).ajaxError(function () {

	})

	if (!validEmail($("#login-email").val())) {
		Swal.fire({
			title: 'Email Invalid',
			text: 'Please enter a valid email address.',
			icon: 'error',
			confirmButtonText: 'OK'
		});
		$("#login-email").focus();
		event.preventDefault;
		return;
	};

	function validEmail(email) {
		var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
		return re.test(email);
	};

	if (($("#login-password").val() === "") || ($("#login-password").val() > 8)) {
		Swal.fire({
			title: 'Password Invalid',
			text: 'Please enter your password.',
			icon: 'error',
			confirmButtonText: 'Ok'
		});
		$("#login-password").focus();
		event.preventDefault;
		return;
	};
	return true;

	// var token = localStorage.getItem("AccessToken");
	// var password = $("#login-password").val();
	// var email = $("#login-email").val();
	// var confirmPassword = $("#signup-password-confirm").val();
	// var form = $(this);
	// var url = form.attr('action');
	// submit.addEventListener('click', validate);

});


// Logout

window.addEventListener('storage', this.syncLogout);







// function showResetPassword()
// {
// 	$("#ForgotPassword").style.display = "block";
// 	$("#Login").style.display = "none";
// 	$("#NewAccount").style.display = "none";	
// };


// function showLogin()
// {
// 	console.log("SHOW LOGIN");
// 	console.log("b4 = " + $("#Login").style.display);

// 	$("#Login").style.display = "block";
// 	$("#ForgotPassword").style.display = "none";
// 	$("#NewAccount").style.display = "none";	

// 	console.log("after = " + $("#Login").style.display);
// };

// function showNewAccount()
// {
// 	$("#NewAccount").style.display = "block";
// 	$("#Login").style.display = "none";
// 	$("#ForgotPassword").style.display = "none";
// };

// function createCookie(key, value, date) {
// 	;
// 	let expiration = new Date(date).toUTCString();
// 	let cookie = escape(key) + "=" + escape(value) + ";expires=" + expiration + ";";
// 	document.cookie = cookie;
// 	console.log(cookie);
// 	console.log("Creating new cookie with key: " + key + " value: " + value + " expiration: " + expiration);
// };



// function readCookie(name) {
// 	let key = name + "=";
// 	let cookies = document.cookie.split(';');

// 	console.log("cookies = " + cookies);

// 	for (let i = 0; i < cookies.length; i++) {
// 		let cookie = cookies[i];
// 		while (cookie.charAt(0) === ' ') {
// 			cookie = cookie.substring(1, cookie.length);
// 		}
// 		if (cookie.indexOf(key) === 0) {
// 			return cookie.substring(key.length, cookie.length);
// 		}
// 	}
// 	return null;
// };


// function createCORSRequest(method, url) {
// 	const xhr = new XMLHttpRequest();
// 	if ("withCredentials" in xhr) {
// 		// Check if the XMLHttpRequest object has a "withCredentials" property.
// 		// "withCredentials" only exists on XMLHTTPRequest2 objects.
// 		console.log("CORS withCredentials");
// 		xhr.open(method, url, true);
// 	} else if (typeof XDomainRequest != "undefined") {
// 		// Otherwise, check if XDomainRequest.
// 		// XDomainRequest only exists in IE, and is IE's way of making CORS requests.
// 		console.log("CORS IE");
// 		xhr = new XDomainRequest();
// 		xhr.open(method, url);
// 	} else {
// 		// Otherwise, CORS is not supported by the browser.
// 		console.log("CORS NOT SUPPORTED");
// 		xhr = null;
// 	}

// 	return xhr;
// }
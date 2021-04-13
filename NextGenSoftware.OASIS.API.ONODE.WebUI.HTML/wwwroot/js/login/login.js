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


// jwt_token = inMemoryToken;
// if (!jwt_token) {
// 	$('#"cd-signin-modal').modal('hide');
// 	removeClass(self.element, 'cd-signin-modal--is-visible');
// }
// return jwt_token

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
		})
		// 	.done(function (data) {
		// 	$.cookie('token', data.token)
		// })
	});


	// $(document).ajaxSuccess(function () {
	// 	localStorage.setItem('access_token', token);
	// 	console.log(response);
	// });

	// $(document).ajaxError(function () {

	// })

	// if (!validEmail($("#login-email").val())) {
	// 	Swal.fire({
	// 		title: 'Email Invalid',
	// 		text: 'Please enter a valid email address.',
	// 		icon: 'error',
	// 		confirmButtonText: 'OK'
	// 	});
	// 	$("#login-email").focus();
	// 	event.preventDefault;
	// 	return;
	// };

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


window.addEventListener('storage', this.syncLogout);

// function readCookie(name) {
// 	let key = name + "=";
// 	let cookies = document.cookie.split(';');

// 	console.log("cookies = " + cookies);





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

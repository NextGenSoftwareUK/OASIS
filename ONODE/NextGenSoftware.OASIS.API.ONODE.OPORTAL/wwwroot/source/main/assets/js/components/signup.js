function onSignup() {
	// Get button and change it when pressed
	const submitBtn = document.getElementById('signup-submit')
	submitBtn.innerHTML = 'loading... <img width="20px" src="assets/img/loading.gif"/>'
	submitBtn.disabled = true
	let n = {
		email: document.getElementById('signup-email').value,
		password: document.getElementById('signup-password').value,
		confirmPassword: document.getElementById('confirm-signup-password').value,
		acceptTerms: !0,
		avatarType: 'User',
	};
	(async () => {
		const e = await fetch(
			'https://api.oasisweb4.one/api/avatar/register',
			{
				method: 'POST',
				body: JSON.stringify(n),
				headers: { 'Content-Type': 'application/json' },
			}
		);
		submitBtn.innerHTML = 'Submit'
		submitBtn.disabled = false

		e.status !== 200 ? submitBtn.classList.add('error'):null

		var t;
		200 === e.status
			? ((t = await e.json()), addAuthPopup(false, t, e))
			: ((t = await e.json()), addAuthPopup(false, t, e)),
			window.location.reload();
	})();
}

function accountDropdown() {
	// Check if device is mobile...
	if( /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) && localStorage.getItem('loggedIn') === "true") {
		// Get dropdown list
		var dropdown = document.getElementsByClassName('nav__sub-list')[0]
		if (dropdown.classList.contains('nav__sub-list--clicked')) {
			dropdown.classList.remove('nav__sub-list--clicked')
			return
		}
		dropdown.classList.add('nav__sub-list--clicked')
	} 
}
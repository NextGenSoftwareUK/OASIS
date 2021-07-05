	//Login/Signup modal window - by CodyHouse.co
(function () {

	function ModalSignin(element) {
		this.element = element;
		this.blocks = this.element.getElementsByClassName('js-signin-modal-block');
		// this.switchers = this.element.getElementsByClassName('js-signin-modal-switcher')[0].getElementsByTagName('a');
		this.triggers = document.getElementsByClassName('js-signin-modal-trigger');
		this.hidePassword = this.element.getElementsByClassName('js-hide-password');
		this.init();
	}

	ModalSignin.prototype.init = function () {
		var self = this;

		//open modal/switch form
		for (var i = 0; i < this.triggers.length; i++) {
			(function (i) {
				self.triggers[i].addEventListener('click', function (event) {
					if (event.target.hasAttribute('data-signin')) {
						event.preventDefault();
						self.showSigninForm(event.target.getAttribute('data-signin'));
					}
				});
			})(i);
		}

		//close modal
		this.element.addEventListener('click', function (event) {
			if (hasClass(event.target, 'js-signin-modal') || hasClass(event.target, 'js-close')) {
				event.preventDefault();
				removeClass(self.element, 'signin-modal--is-visible');
				document.querySelector('form').reset();
			}
		});

		//close modal when clicking the esc keyboard button
		document.addEventListener('keydown', function (event) {
			(event.target == '27') && self.element.removeClass('signin-modal--is-visible');
		}
		);

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
		// target.textContent = ( 'Show' == target.textContent ) ? 'Hide' : 'Show';
		('password' == password.getAttribute('type')) ? target.setAttribute('style', '-webkit-mask-image: url(/secondary-expandable-navigation-master/img/visible-icon.svg)'): target.setAttribute('style', '-webkit-mask-image: url(/secondary-expandable-navigation-master/img/hidden-icon.svg)');
		putCursorAtEnd(password);
	};

	ModalSignin.prototype.showSigninForm = function (type) {
		// show modal if not visible
		!hasClass(this.element, 'signin-modal--is-visible') && addClass(this.element, 'signin-modal--is-visible');
		// show selected form
		for (var i = 0; i < this.blocks.length; i++) {
			this.blocks[i].getAttribute('data-type') == type ? addClass(this.blocks[i], 'signin-modal__block--is-selected') : removeClass(this.blocks[i], 'signin-modal__block--is-selected');
		}
		//update switcher appearance
		// var switcherType = (type == 'signup') ? 'signup' : 'login';
		// for( var i=0; i < this.switchers.length; i++ ) {
		// 	this.switchers[i].getAttribute('data-type') == switcherType ? addClass(this.switchers[i], 'selected') : removeClass(this.switchers[i], 'selected');
		// }
	};

	var signinModal = document.getElementsByClassName("js-signin-modal")[0];
	if (signinModal) {
		new ModalSignin(signinModal);
	}

	// toggle main navigation on mobile
	var mainNav = document.getElementsByClassName('js-main-nav')[0];
	if (mainNav) {
		mainNav.addEventListener('click', function (event) {
			if (hasClass(event.target, 'js-main-nav')); {
				var navList = mainNav.getElementsByTagName('ul')[0];
				toggleClass(navList, 'main-nav__list--is-visible', !hasClass(navList, 'main-nav__list--is-visible'));
			}
		});
	}
})();
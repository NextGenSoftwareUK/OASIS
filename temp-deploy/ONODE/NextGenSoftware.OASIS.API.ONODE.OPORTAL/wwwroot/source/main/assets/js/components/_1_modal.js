// File#: _2_modal


	var ModalSignin = function (element) {
		this.element = element;
		this.blocks = document.getElementsByClassName('js-modal-block');
		this.triggers = document.getElementsByClassName('js-modal-trigger');
		this.init();
	};

	ModalSignin.prototype.init = function () {
		var self = this;

		// open modal
		for (var i = 0; i < this.triggers.length; i++) {
			(function (i) {
				self.triggers[i].addEventListener('click', function (event) {
					if (event.target.hasAttribute('data-signin')) {
						// event.preventDefault();
						self.showSigninForm(event.target.getAttribute('data-signin'));
					}
				});
			})(i);
		}

		// close modal

		// TODO : add function to reset inputs after modal is closed

		this.element.addEventListener('click', function (event) {
			if (hasClass(event.target, 'js-modal')) {
				event.preventDefault();
				removeClass(self.element, 'is-visible');
			}
		});

		// close modal when clicking the esc keyboard button
		document.addEventListener('keyup', function (event) {
			if (event.key == 'Escape') {
				removeClass(self.element, 'is-visible');
				removeClass(self.blocks, 'is-selected');
			}
		});
	};

	ModalSignin.prototype.showSigninForm = function (type) {
		// show modal if not visible
		!hasClass(this.element, 'is-visible') &&
			addClass(this.element, 'is-visible');
		// show selected form
		for (var i = 0; i < this.blocks.length; i++) {
			this.blocks[i].getAttribute('data-type') == type
				? addClass(this.blocks[i], 'is-selected')
				: removeClass(this.blocks[i], 'is-selected');
		}
	};

	ModalSignin.prototype.toggleError = function (input, bool) {
		// used to show error messages in the form
		toggleClass(input, 'modal__input--has-error', bool);
		toggleClass(input.nextElementSibling, 'modal__error--is-visible', bool);
	};

	var signinModal = document.getElementsByClassName('js-modal')[0];
	if (signinModal) {
		new ModalSignin(signinModal);
	}

//class manipulations - needed if classList is not supported
function hasClass(el, className) {
	if (el.classList) return el.classList.contains(className);
	else
		return !!el.className.match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
}

function addClass(el, className) {
	var classList = className.split(' ');
	if (el.classList) el.classList.add(classList[0]);
	else if (!hasClass(el, classList[0])) el.className += ' ' + classList[0];
	if (classList.length > 1) addClass(el, classList.slice(1).join(' '));
}

function removeClass(el, className) {
	var classList = className.split(' ');
	if (el.classList) el.classList.remove(classList[0]);
	else if (hasClass(el, classList[0])) {
		var reg = new RegExp('(\\s|^)' + classList[0] + '(\\s|$)');
		el.className = el.className.replace(reg, ' ');
	}
	if (classList.length > 1) removeClass(el, classList.slice(1).join(' '));
}

function toggleClass(el, className, bool) {
	if (bool) addClass(el, className);
	else removeClass(el, className);
}

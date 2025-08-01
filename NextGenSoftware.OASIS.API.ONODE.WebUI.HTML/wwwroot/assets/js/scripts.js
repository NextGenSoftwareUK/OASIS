// File: util.js

function Util() {};

/*
	class manipulation functions
*/

Util.hasClass = function (el, className) {
  if (el.classList) return el.classList.contains(className);
  else return !!el.getAttribute('class').match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
};

Util.addClass = function (el, className) {
  var classList = className.split(' ');
  if (el.classList) el.classList.add(classList[0]);
  else if (!Util.hasClass(el, classList[0])) el.setAttribute('class', el.getAttribute('class') + " " + classList[0]);
  if (classList.length > 1) Util.addClass(el, classList.slice(1).join(' '));
};

Util.removeClass = function (el, className) {
  var classList = className.split(' ');
  if (el.classList) el.classList.remove(classList[0]);
  else if (Util.hasClass(el, classList[0])) {
    var reg = new RegExp('(\\s|^)' + classList[0] + '(\\s|$)');
    el.setAttribute('class', el.getAttribute('class').replace(reg, ' '));
  }
  if (classList.length > 1) Util.removeClass(el, classList.slice(1).join(' '));
};

Util.toggleClass = function (el, className, bool) {
  if (bool) Util.addClass(el, className);
  else Util.removeClass(el, className);
};

Util.setAttributes = function (el, attrs) {
  for (var key in attrs) {
    el.setAttribute(key, attrs[key]);
  }
};

/*
  DOM manipulation
*/

Util.getChildrenByClassName = function (el, className) {
  var children = el.children,
    childrenByClass = [];
  for (var i = 0; i < el.children.length; i++) {
    if (Util.hasClass(el.children[i], className)) childrenByClass.push(el.children[i]);
  }
  return childrenByClass;
};

Util.is = function (elem, selector) {
  if (selector.nodeType) {
    return elem === selector;
  }

  var qa = (typeof (selector) === 'string' ? document.querySelectorAll(selector) : selector),
    length = qa.length,
    returnArr = [];

  while (length--) {
    if (qa[length] === elem) {
      return true;
    }
  }

  return false;
};

/*
	Animate height of an element
*/

Util.setHeight = function (start, to, element, duration, cb, timeFunction) {
  var change = to - start,
    currentTime = null;

  var animateHeight = function (timestamp) {
    if (!currentTime) currentTime = timestamp;
    var progress = timestamp - currentTime;
    if (progress > duration) progress = duration;
    var val = parseInt((progress / duration) * change + start);
    if (timeFunction) {
      val = Math[timeFunction](progress, start, to - start, duration);
    }
    element.style.height = val + "px";
    if (progress < duration) {
      window.requestAnimationFrame(animateHeight);
    } else {
      if (cb) cb();
    }
  };

  //set the height of the element before starting animation -> fix bug on Safari
  element.style.height = start + "px";
  window.requestAnimationFrame(animateHeight);
};

/*
	Smooth Scroll
*/

Util.scrollTo = function (final, duration, cb, scrollEl) {
  var element = scrollEl || window;
  var start = element.scrollTop || document.documentElement.scrollTop,
    currentTime = null;

  if (!scrollEl) start = window.scrollY || document.documentElement.scrollTop;

  var animateScroll = function (timestamp) {
    if (!currentTime) currentTime = timestamp;
    var progress = timestamp - currentTime;
    if (progress > duration) progress = duration;
    var val = Math.easeInOutQuad(progress, start, final - start, duration);
    element.scrollTo(0, val);
    if (progress < duration) {
      window.requestAnimationFrame(animateScroll);
    } else {
      cb && cb();
    }
  };

  window.requestAnimationFrame(animateScroll);
};

/*
  Focus utility classes
*/

//Move focus to an element
Util.moveFocus = function (element) {
  if (!element) element = document.getElementsByTagName("body")[0];
  element.focus();
  if (document.activeElement !== element) {
    element.setAttribute('tabindex', '-1');
    element.focus();
  }
};

/*
  Misc
*/

Util.getIndexInArray = function (array, el) {
  return Array.prototype.indexOf.call(array, el);
};

Util.cssSupports = function (property, value) {
  if ('CSS' in window) {
    return CSS.supports(property, value);
  } else {
    var jsProperty = property.replace(/-([a-z])/g, function (g) {
      return g[1].toUpperCase();
    });
    return jsProperty in document.body.style;
  }
};

// merge a set of user options into plugin defaults
// https://gomakethings.com/vanilla-javascript-version-of-jquery-extend/
Util.extend = function () {
  // Variables
  var extended = {};
  var deep = false;
  var i = 0;
  var length = arguments.length;

  // Check if a deep merge
  if (Object.prototype.toString.call(arguments[0]) === '[object Boolean]') {
    deep = arguments[0];
    i++;
  }

  // Merge the object into the extended object
  var merge = function (obj) {
    for (var prop in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, prop)) {
        // If deep merge and property is an object, merge properties
        if (deep && Object.prototype.toString.call(obj[prop]) === '[object Object]') {
          extended[prop] = extend(true, extended[prop], obj[prop]);
        } else {
          extended[prop] = obj[prop];
        }
      }
    }
  };

  // Loop through each object and conduct a merge
  for (; i < length; i++) {
    var obj = arguments[i];
    merge(obj);
  }

  return extended;
};

// Check if Reduced Motion is enabled
Util.osHasReducedMotion = function () {
  if (!window.matchMedia) return false;
  var matchMediaObj = window.matchMedia('(prefers-reduced-motion: reduce)');
  if (matchMediaObj) return matchMediaObj.matches;
  return false; // return false if not supported
};

/*
	Polyfills
*/
//Closest() method
if (!Element.prototype.matches) {
  Element.prototype.matches = Element.prototype.msMatchesSelector || Element.prototype.webkitMatchesSelector;
}

if (!Element.prototype.closest) {
  Element.prototype.closest = function (s) {
    var el = this;
    if (!document.documentElement.contains(el)) return null;
    do {
      if (el.matches(s)) return el;
      el = el.parentElement || el.parentNode;
    } while (el !== null && el.nodeType === 1);
    return null;
  };
}

//Custom Event() constructor
if (typeof window.CustomEvent !== "function") {

  function CustomEvent(event, params) {
    params = params || {
      bubbles: false,
      cancelable: false,
      detail: undefined
    };
    var evt = document.createEvent('CustomEvent');
    evt.initCustomEvent(event, params.bubbles, params.cancelable, params.detail);
    return evt;
  }

  CustomEvent.prototype = window.Event.prototype;

  window.CustomEvent = CustomEvent;
}

/*
	Animation curves
*/
Math.easeInOutQuad = function (t, b, c, d) {
  t /= d / 2;
  if (t < 1) return c / 2 * t * t + b;
  t--;
  return -c / 2 * (t * (t - 2) - 1) + b;
};

Math.easeInQuart = function (t, b, c, d) {
  t /= d;
  return c * t * t * t * t + b;
};

Math.easeOutQuart = function (t, b, c, d) {
  t /= d;
  t--;
  return -c * (t * t * t * t - 1) + b;
};

Math.easeInOutQuart = function (t, b, c, d) {
  t /= d / 2;
  if (t < 1) return c / 2 * t * t * t * t + b;
  t -= 2;
  return -c / 2 * (t * t * t * t - 2) + b;
};

Math.easeOutElastic = function (t, b, c, d) {
  var s = 1.70158;
  var p = d * 0.7;
  var a = c;
  if (t == 0) return b;
  if ((t /= d) == 1) return b + c;
  if (!p) p = d * .3;
  if (a < Math.abs(c)) {
    a = c;
    var s = p / 4;
  } else var s = p / (2 * Math.PI) * Math.asin(c / a);
  return a * Math.pow(2, -10 * t) * Math.sin((t * d - s) * (2 * Math.PI) / p) + c + b;
};


/* JS Utility Classes */

// make focus ring visible only for keyboard navigation (i.e., tab key)
(function () {
  var focusTab = document.getElementsByClassName('js-tab-focus'),
    shouldInit = false,
    outlineStyle = false,
    eventDetected = false;

  function detectClick() {
    if (focusTab.length > 0) {
      resetFocusStyle(false);
      window.addEventListener('keydown', detectTab);
    }
    window.removeEventListener('mousedown', detectClick);
    outlineStyle = false;
    eventDetected = true;
  };

  function detectTab(event) {
    if (event.keyCode !== 9) return;
    resetFocusStyle(true);
    window.removeEventListener('keydown', detectTab);
    window.addEventListener('mousedown', detectClick);
    outlineStyle = true;
  };

  function resetFocusStyle(bool) {
    var outlineStyle = bool ? '' : 'none';
    for (var i = 0; i < focusTab.length; i++) {
      focusTab[i].style.setProperty('outline', outlineStyle);
    }
  };

  function initFocusTabs() {
    if (shouldInit) {
      if (eventDetected) resetFocusStyle(outlineStyle);
      return;
    }
    shouldInit = focusTab.length > 0;
    window.addEventListener('mousedown', detectClick);
  };

  initFocusTabs();
  window.addEventListener('initFocusTabs', initFocusTabs);
}());

function resetFocusTabsStyle() {
  window.dispatchEvent(new CustomEvent('initFocusTabs'));
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

// File#: _1_password

(function () {
  var Password = function (element) {
    this.element = element;
    this.password = this.element.getElementsByClassName('js-password__input')[0];
    this.visibilityBtn = this.element.getElementsByClassName('js-password__btn')[0];
    this.visibilityClass = 'password--text-is-visible';
    this.initPassword();
  };

  Password.prototype.initPassword = function () {
    var self = this;
    
    //listen to the click on the password btn
    this.visibilityBtn.addEventListener('click', function (event) {
      //if password is in focus -> do nothing if user presses Enter
      if (document.activeElement === self.password) return;
      event.preventDefault();
      self.togglePasswordVisibility();
    });
  };

  Password.prototype.togglePasswordVisibility = function () {
    var makeVisible = !Util.hasClass(this.element, this.visibilityClass);
    //change element class
    Util.toggleClass(this.element, this.visibilityClass, makeVisible);
    //change input type
    (makeVisible) ? this.password.setAttribute('type', 'text'): this.password.setAttribute('type', 'password');
  };

  //initialize the Password objects
  var passwords = document.getElementsByClassName('js-password');
  if (passwords.length > 0) {
    for (var i = 0; i < passwords.length; i++) {
      (function (i) {
        new Password(passwords[i]);
      })(i);
    }
  };
}());
// File#: _2_menu

jQuery(document).ready(function ($) {
	var $menu_trigger = $('.js-nav-trigger'),
		$body = $('body'),
		$navigation = $('.side-nav');

	//open-close lateral menu clicking on the menu icon
	$menu_trigger.on('click', function (event) {
		event.preventDefault();

		// TODO : menu no longer closes when clicking outside it

		$menu_trigger.toggleClass('is-clicked');
		$navigation.toggleClass('is-visible');
		$('.item--has-children').children('a').removeClass('submenu-open').next('.sub-menu').delay(300).slideUp();
		// $body.toggleClass('is-visible').one('webkitTransitionEnd otransitionend oTransitionEnd msTransitionEnd transitionend', function () {
		// firefox transitions break when parent overflow is changed, so we need to wait for the end of the trasition to give the body an overflow hidden
		// $('.side-nav').toggleClass('is-visible');

		//check if transitions are not supported - i.e. in IE9
		// if ($('html').hasClass('no-csstransitions')) {
		// }
	});

	//close lateral menu clicking outside the menu itself
	$body.on('click', function (event) {
		// event.preventDefault();
		if ($(event.target).is($body) || ($(event.which).is('27'))) {
			$menu_trigger.removeClass('is-clicked'),
				$navigation.removeClass('is-visible'),
				$('.item--has-children').children('a').removeClass('submenu-open').next('.sub-menu').delay(300).slideUp();
		};
	});

	$body.on('keydown', function (event) {
		// event.preventDefault();
		if (event.key === "Escape") {
			$menu_trigger.removeClass('is-clicked');
			$navigation.removeClass('is-visible');
			$('.item--has-children').children('a').removeClass('submenu-open').next('.sub-menu').delay(300).slideUp();
		}

	});

	//open (or close) submenu items in the lateral menu. Close all the other open submenu items.
	$('.item--has-children').children('a').on('click', function (event) {
		event.preventDefault();
		$(this).toggleClass('submenu-open').next('.sub-menu').slideToggle(300).end().parent('.item--has-children').siblings('.item--has-children').children('a').removeClass('submenu-open').next('.sub-menu').slideUp(300);
	});
});
function setup() {
  var user;
  if (localStorage.getItem('avatar'))
    user = JSON.parse(localStorage.getItem('avatar'))
  var loginDiv = document.querySelector('[data-display="loggedIn"]')
  var avatarDiv = document.querySelector('.nav__item--account')
  var icon = document.getElementsByClassName('avatar')[0]

  /*if logged in, hide guest links*/
  if (localStorage.getItem('loggedIn') === "true"){
    var guest_links = document.getElementById('guest-links')
    var username = document.getElementById("username")
    guest_links.style.display = "none"
    avatarDiv.classList.add('loggedin')
    username.innerHTML = user.username
    icon.src='assets/img/loggedin.png'
  }

  /* Do not show drop down*/
  else{
    // loginDiv.style.display = 'none'
    if (avatarDiv.classList.contains('loggedin')) {
      avatarDiv.classList.remove('loggedin')
    }
   icon.src='assets/img/loggedout.png' 
  }
}

function addAuthPopup(login, msg, e) {
	// Get and remove previous pop ups
	var prev = document.getElementsByClassName('alert')[0]
	if (prev)prev.remove()
	console.log(msg)
	var formId;
	var type;
	var alert = msg.message || msg.title;
	if (msg.isError || msg.status === 400){ 
		type = 'error'
		// alert = msg.title;
	}
	else {
		type = 'success'
		localStorage.setItem('avatar', JSON.stringify(msg.avatar));
		localStorage.setItem('loggedIn', true)
		//Reloads page after 5sec
		setTimeout(()=>window.location.reload(), 5000)
	}
	login ? formId = 'login-form' : formId = 'signup-form'
		// Create popup element
		let target = document.getElementById(formId)
		var div = document.createElement('div');
		div.classList.add('alert')
		div.classList.add(type)
		div.innerHTML = alert;
		target.parentNode.insertBefore(div, target)	
		console.log(type)
		e.preventDefault()
}
function onLogin() {
	// Get button and change it when pressed
	const submitBtn = document.getElementById('login-submit')
	submitBtn.innerHTML = 'logging in... <img width="20px" src="assets/img/loading.gif"/>'
	submitBtn.disabled = true
	let n = {
		email: document.getElementById('login-email').value,
		password: document.getElementById('login-password').value,
	};
	(async () => {
		const e = await fetch(
			'https://api.oasisweb4.one/api/avatar/authenticate',
			{
				method: 'POST',
				body: JSON.stringify(n),
				headers: { 'Content-Type': 'application/json' },
			}
		);
		// Re-enable button after request
		submitBtn.innerHTML = 'Submit'
		submitBtn.disabled = false
		var t;
		200 === e.status
			? ((t = await e.json()), addAuthPopup(true, t, e))
			: ((submitBtn.classList.add('error')), (t = await e.json()), addAuthPopup(true, t, e)),
			window.location.reload();
	})();
}

async function onLogout() {
	const user = JSON.parse(localStorage.getItem('avatar'))
	console.log(JSON.parse(localStorage.getItem('avatar')))
	const body = {token: user.jwtToken}
	const loading = document.getElementById('loading')
	
	loading.classList.add('modal')
	loading.classList.add('is-visible')
	loading.innerHTML = '<img src="assets/img/loading.gif"/>'
	console.log(body)

	const e = await fetch('https://api.oasisweb4.one/api/avatar/revoke-token', 
	{
		method: 'POST',
		body: JSON.stringify(body),
		headers: {
			'Content-Type': 'application/json'
		}
	})
	localStorage.removeItem('avatar')
	localStorage.setItem('loggedIn', false)
	window.location.reload()
}
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

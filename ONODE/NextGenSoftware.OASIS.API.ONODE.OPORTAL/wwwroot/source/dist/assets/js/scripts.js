function Util() {}
(Util.hasClass = function (e, t) {
	return e.classList
		? e.classList.contains(t)
		: !!e.getAttribute('class').match(new RegExp('(\\s|^)' + t + '(\\s|$)'));
}),
	(Util.addClass = function (e, t) {
		t = t.split(' ');
		e.classList
			? e.classList.add(t[0])
			: Util.hasClass(e, t[0]) ||
			  e.setAttribute('class', e.getAttribute('class') + ' ' + t[0]),
			1 < t.length && Util.addClass(e, t.slice(1).join(' '));
	}),
	(Util.removeClass = function (e, t) {
		var n = t.split(' ');
		e.classList
			? e.classList.remove(n[0])
			: Util.hasClass(e, n[0]) &&
			  ((t = new RegExp('(\\s|^)' + n[0] + '(\\s|$)')),
			  e.setAttribute('class', e.getAttribute('class').replace(t, ' '))),
			1 < n.length && Util.removeClass(e, n.slice(1).join(' '));
	}),
	(Util.toggleClass = function (e, t, n) {
		n ? Util.addClass(e, t) : Util.removeClass(e, t);
	}),
	(Util.setAttributes = function (e, t) {
		for (var n in t) e.setAttribute(n, t[n]);
	}),
	(Util.getChildrenByClassName = function (e, t) {
		e.children;
		for (var n = [], s = 0; s < e.children.length; s++)
			Util.hasClass(e.children[s], t) && n.push(e.children[s]);
		return n;
	}),
	(Util.is = function (e, t) {
		if (t.nodeType) return e === t;
		for (
			var n = 'string' == typeof t ? document.querySelectorAll(t) : t,
				s = n.length;
			s--;

		)
			if (n[s] === e) return !0;
		return !1;
	}),
	(Util.setHeight = function (n, s, i, o, a, l) {
		var r = s - n,
			c = null,
			u = function (e) {
				var t = e - (c = c || e);
				o < t && (t = o);
				e = parseInt((t / o) * r + n);
				l && (e = Math[l](t, n, s - n, o)),
					(i.style.height = e + 'px'),
					t < o ? window.requestAnimationFrame(u) : a && a();
			};
		(i.style.height = n + 'px'), window.requestAnimationFrame(u);
	}),
	(Util.scrollTo = function (n, s, i, e) {
		var o = e || window,
			a = o.scrollTop || document.documentElement.scrollTop,
			l = null;
		e || (a = window.scrollY || document.documentElement.scrollTop);
		var r = function (e) {
			var t = e - (l = l || e);
			s < t && (t = s);
			e = Math.easeInOutQuad(t, a, n - a, s);
			o.scrollTo(0, e), t < s ? window.requestAnimationFrame(r) : i && i();
		};
		window.requestAnimationFrame(r);
	}),
	(Util.moveFocus = function (e) {
		(e = e || document.getElementsByTagName('body')[0]).focus(),
			document.activeElement !== e &&
				(e.setAttribute('tabindex', '-1'), e.focus());
	}),
	(Util.getIndexInArray = function (e, t) {
		return Array.prototype.indexOf.call(e, t);
	}),
	(Util.cssSupports = function (e, t) {
		return 'CSS' in window
			? CSS.supports(e, t)
			: e.replace(/-([a-z])/g, function (e) {
					return e[1].toUpperCase();
			  }) in document.body.style;
	}),
	(Util.extend = function () {
		var n = {},
			s = !1,
			e = 0,
			t = arguments.length;
		'[object Boolean]' === Object.prototype.toString.call(arguments[0]) &&
			((s = arguments[0]), e++);
		for (; e < t; e++)
			!(function (e) {
				for (var t in e)
					Object.prototype.hasOwnProperty.call(e, t) &&
						(s && '[object Object]' === Object.prototype.toString.call(e[t])
							? (n[t] = extend(!0, n[t], e[t]))
							: (n[t] = e[t]));
			})(arguments[e]);
		return n;
	}),
	(Util.osHasReducedMotion = function () {
		if (!window.matchMedia) return !1;
		var e = window.matchMedia('(prefers-reduced-motion: reduce)');
		return !!e && e.matches;
	}),
	Element.prototype.matches ||
		(Element.prototype.matches =
			Element.prototype.msMatchesSelector ||
			Element.prototype.webkitMatchesSelector),
	Element.prototype.closest ||
		(Element.prototype.closest = function (e) {
			var t = this;
			if (!document.documentElement.contains(t)) return null;
			do {
				if (t.matches(e)) return t;
			} while (
				null !== (t = t.parentElement || t.parentNode) &&
				1 === t.nodeType
			);
			return null;
		});
{
	function CustomEvent(e, t) {
		t = t || { bubbles: !1, cancelable: !1, detail: void 0 };
		var n = document.createEvent('CustomEvent');
		return n.initCustomEvent(e, t.bubbles, t.cancelable, t.detail), n;
	}
	'function' != typeof window.CustomEvent &&
		((CustomEvent.prototype = window.Event.prototype),
		(window.CustomEvent = CustomEvent));
}
function resetFocusTabsStyle() {
	window.dispatchEvent(new CustomEvent('initFocusTabs'));
}
function putCursorAtEnd(e) {
	var t;
	e.setSelectionRange
		? ((t = 2 * e.value.length), e.focus(), e.setSelectionRange(t, t))
		: (e.value = e.value);
}
function onLogin() {
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
		var t;
		200 === e.status
			? ((t = await e.json()), alert(t.message))
			: ((t = await e.json()), alert(t.title)),
			window.location.reload();
	})();
}
function onSignup() {
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
		var t;
		200 === e.status
			? ((t = await e.json()), alert(t.message))
			: ((t = await e.json()), alert(t.title)),
			window.location.reload();
	})();
}
(Math.easeInOutQuad = function (e, t, n, s) {
	return (e /= s / 2) < 1
		? (n / 2) * e * e + t
		: (-n / 2) * (--e * (e - 2) - 1) + t;
}),
	(Math.easeInQuart = function (e, t, n, s) {
		return n * (e /= s) * e * e * e + t;
	}),
	(Math.easeOutQuart = function (e, t, n, s) {
		return (e /= s), -n * (--e * e * e * e - 1) + t;
	}),
	(Math.easeInOutQuart = function (e, t, n, s) {
		return (e /= s / 2) < 1
			? (n / 2) * e * e * e * e + t
			: (-n / 2) * ((e -= 2) * e * e * e - 2) + t;
	}),
	(Math.easeOutElastic = function (e, t, n, s) {
		var i = 1.70158,
			o = 0.7 * s,
			a = n;
		return 0 == e
			? t
			: 1 == (e /= s)
			? t + n
			: ((o = o || 0.3 * s),
			  (i =
					a < Math.abs(n)
						? ((a = n), o / 4)
						: (o / (2 * Math.PI)) * Math.asin(n / a)),
			  a * Math.pow(2, -10 * e) * Math.sin(((e * s - i) * (2 * Math.PI)) / o) +
					n +
					t);
	}),
	(function () {
		var s = document.getElementsByClassName('js-tab-focus'),
			e = !1,
			t = !1,
			n = !1;
		function i() {
			0 < s.length && (a(!1), window.addEventListener('keydown', o)),
				window.removeEventListener('mousedown', i),
				(n = !(t = !1));
		}
		function o(e) {
			9 === e.keyCode &&
				(a(!0),
				window.removeEventListener('keydown', o),
				window.addEventListener('mousedown', i),
				(t = !0));
		}
		function a(e) {
			for (var t = e ? '' : 'none', n = 0; n < s.length; n++)
				s[n].style.setProperty('outline', t);
		}
		function l() {
			e
				? n && a(t)
				: ((e = 0 < s.length), window.addEventListener('mousedown', i));
		}
		l(), window.addEventListener('initFocusTabs', l);
	})(),
	(function () {
		function e(e) {
			(this.element = e),
				(this.password =
					this.element.getElementsByClassName('js-password__input')[0]),
				(this.visibilityBtn =
					this.element.getElementsByClassName('js-password__btn')[0]),
				(this.visibilityClass = 'password--text-is-visible'),
				this.initPassword();
		}
		(e.prototype.initPassword = function () {
			var t = this;
			this.visibilityBtn.addEventListener('click', function (e) {
				document.activeElement !== t.password &&
					(e.preventDefault(), t.togglePasswordVisibility());
			});
		}),
			(e.prototype.togglePasswordVisibility = function () {
				var e = !Util.hasClass(this.element, this.visibilityClass);
				Util.toggleClass(this.element, this.visibilityClass, e),
					e
						? this.password.setAttribute('type', 'text')
						: this.password.setAttribute('type', 'password');
			});
		var t = document.getElementsByClassName('js-password');
		if (0 < t.length) for (var n = 0; n < t.length; n++) new e(t[n]);
	})(),
	jQuery(document).ready(function (t) {
		var n = t('.js-nav-trigger'),
			s = t('body'),
			i = t('.side-nav');
		n.on('click', function (e) {
			e.preventDefault(),
				n.toggleClass('is-clicked'),
				i.toggleClass('is-visible'),
				t('.item--has-children')
					.children('a')
					.removeClass('submenu-open')
					.next('.sub-menu')
					.delay(300)
					.slideUp();
		}),
			s.on('click', function (e) {
				e.preventDefault(),
					(t(e.target).is(s) || t(e.which).is('27')) &&
						(n.removeClass('is-clicked'),
						i.removeClass('is-visible'),
						t('.item--has-children')
							.children('a')
							.removeClass('submenu-open')
							.next('.sub-menu')
							.delay(300)
							.slideUp());
			}),
			s.on('keydown', function (e) {
				e.preventDefault(),
					t(e.which).is('27') &&
						(n.removeClass('is-clicked'),
						i.removeClass('is-visible'),
						t('.item--has-children')
							.children('a')
							.removeClass('submenu-open')
							.next('.sub-menu')
							.delay(300)
							.slideUp());
			}),
			t('.item--has-children')
				.children('a')
				.on('click', function (e) {
					e.preventDefault(),
						t(this)
							.toggleClass('submenu-open')
							.next('.sub-menu')
							.slideToggle(300)
							.end()
							.parent('.item--has-children')
							.siblings('.item--has-children')
							.children('a')
							.removeClass('submenu-open')
							.next('.sub-menu')
							.slideUp(300);
				});
	}),
	jQuery(document).ready(function (e) {
		function t(e) {
			(this.element = e),
				(this.blocks = document.getElementsByClassName('js-modal-block')),
				(this.triggers = document.getElementsByClassName('js-modal-trigger')),
				this.init();
		}
		(t.prototype.init = function () {
			for (var e, t = this, n = 0; n < this.triggers.length; n++)
				(e = n),
					t.triggers[e].addEventListener('click', function (e) {
						e.target.hasAttribute('data-signin') &&
							(e.preventDefault(),
							t.showSigninForm(e.target.getAttribute('data-signin')));
					});
			this.element.addEventListener('click', function (e) {
				s(e.target, 'js-modal') &&
					(e.preventDefault(), o(t.element, 'is-visible'));
			}),
				document.addEventListener('keyup', function (e) {
					alert("jj")
					27 == e.keyCode && o(t.element, 'is-visible');
				});
		}),
			(t.prototype.showSigninForm = function (e) {
				s(this.element, 'is-visible') || i(this.element, 'is-visible');
				for (var t = 0; t < this.blocks.length; t++)
					(this.blocks[t].getAttribute('data-type') == e ? i : o)(
						this.blocks[t],
						'is-selected'
					);
			}),
			(t.prototype.toggleError = function (e, t) {
				a(e, 'modal__input--has-error', t),
					a(e.nextElementSibling, 'modal__error--is-visible', t);
			});
		var n = document.getElementsByClassName('js-modal')[0];
		function s(e, t) {
			return e.classList
				? e.classList.contains(t)
				: e.className.match(new RegExp('(\\s|^)' + t + '(\\s|$)'));
		}
		function i(e, t) {
			t = t.split(' ');
			e.classList
				? e.classList.add(t[0])
				: s(e, t[0]) || (e.className += ' ' + t[0]),
				1 < t.length && i(e, t.slice(1).join(' '));
		}
		function o(e, t) {
			var n = t.split(' ');
			e.classList
				? e.classList.remove(n[0])
				: s(e, n[0]) &&
				  ((t = new RegExp('(\\s|^)' + n[0] + '(\\s|$)')),
				  (e.className = e.className.replace(t, ' '))),
				1 < n.length && o(e, n.slice(1).join(' '));
		}
		function a(e, t, n) {
			(n ? i : o)(e, t);
		}
		n && new t(n);
	});
//# sourceMappingURL=scripts.js.map

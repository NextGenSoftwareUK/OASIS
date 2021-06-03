
(function () {
    //Login/Signup modal window - by CodyHouse.co
    function ModalLogin(element) {
        this.element = element;
        this.blocks = this.element.getElementsByClassName('login-modal-block');
        this.switchers = this.element.getElementsByClassName('login-modal-switcher')[0].getElementsByTagName('a');
        this.triggers = document.getElementsByClassName('login-modal-trigger');
        this.hidePassword = this.element.getElementsByClassName('hide-password');
        this.init();
    };

    ModalLogin.prototype.init = function () {
        var self = this;
        //open modal/switch form
        for (var i = 0; i < this.triggers.length; i++) {
            (function (i) {
                self.triggers[i].addEventListener('click', function (event) {
                    if (event.target.hasAttribute('data-login')) {
                        event.preventDefault();
                        self.showLoginForm(event.target.getAttribute('data-login'));
                    }
                });
            })(i);
        }

        //close modal
        this.element.addEventListener('click', function (event) {
            if (hasClass(event.target, 'login-modal') || hasClass(event.target, 'login-modal-close')) {
                event.preventDefault();
                removeClass(self.element, 'login-modal--is-visible');
            }
        });
        //close modal when clicking the esc keyboard button
        document.addEventListener('keydown', function (event) {
            if (event == '27') {
                removeClass(self.element, 'login-modal--is-visible');
            }
        });

        //hide/show password
        for (var i = 0; i < this.hidePassword.length; i++) {
            (function (i) {
                self.hidePassword[i].addEventListener('click', function (event) {
                    self.togglePassword(self.hidePassword[i]);
                });
            })(i);
        }

        //IMPORTANT - REMOVE THIS - it's just to show/hide error messages in the demo
        // this.blocks[0].getElementsByTagName('form')[0].addEventListener('submit', function (event) {
        //     event.preventDefault();
        //     self.toggleError(document.getElementById('signin-email'), true);
        // });
        // this.blocks[1].getElementsByTagName('form')[0].addEventListener('submit', function (event) {
        //     event.preventDefault();
        //     self.toggleError(document.getElementById('signup-username'), true);
        // });
    };

    ModalLogin.prototype.togglePassword = function (target) {
        var password = target.previousElementSibling;
        ('password' == password.getAttribute('type')) ? password.setAttribute('type', 'text'): password.setAttribute('type', 'password');
        target.textContent = ('Hide' == target.textContent) ? 'Show' : 'Hide';
        putCursorAtEnd(password);
    }

    ModalLogin.prototype.showLoginForm = function (type) {
        // show modal if not visible
        !hasClass(this.element, 'login-modal--is-visible') && addClass(this.element, 'login-modal--is-visible');
        // show selected form
        for (var i = 0; i < this.blocks.length; i++) {
            this.blocks[i].getAttribute('data-type') == type ? addClass(this.blocks[i], 'login-modal__block--is-selected') : removeClass(this.blocks[i], 'login-modal__block--is-selected');
        }
        //update switcher appearance
        var switcherType = (type == 'signup') ? 'signup' : 'login';
        for (var i = 0; i < this.switchers.length; i++) {
            this.switchers[i].getAttribute('data-type') == switcherType ? addClass(this.switchers[i], 'selected') : removeClass(this.switchers[i], 'selected');
        }
    };

    ModalLogin.prototype.toggleError = function (input, bool) {
        // used to show error messages in the form
        toggleClass(input, 'login-modal__input--has-error', bool);
        toggleClass(input.nextElementSibling, 'login-modal__error--is-visible', bool);
    }

    var loginModal = document.getElementsByClassName("login-modal")[0];
    if (loginModal) {
        new ModalLogin(loginModal);
    }

    // toggle main navigation on mobile
    // var mainNav = document.getElementsByClassName('main-nav')[0];
    // if (mainNav) {
    //     mainNav.addEventListener('click', function (event) {
    //         if (hasClass(event.target, 'menu-trigger')) {
    //             var navList = mainNav.getElementByID('menu-trigger')[0];
    //             toggleClass(navList, 'main-nav__list--is-visible', !hasClass(navList, 'main-nav__list--is-visible'));
    //         }
    //     });
    // }

    //class manipulations - needed if classList is not supported
    function hasClass(el, className) {
        if (el.classList) return el.classList.contains(className);
        else return !!el.className.match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
    }

    function addClass(el, className) {
        var classList = className.split(' ');
        if (el.classList) el.classList.add(classList[0]);
        else if (!hasClass(el, classList[0])) el.className += " " + classList[0];
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


    var $lateral_menu_trigger = $('#menu-trigger'),
        $content_wrapper = $('.main-content'),
        $navigation = $('header');
    $items = $('.lateral-nav__item');

    //open-close lateral menu clicking on the menu icon
    $lateral_menu_trigger.on('click', function (event) {
        event.preventDefault();

        $lateral_menu_trigger.toggleClass('is-clicked');
        $navigation.toggleClass('lateral-menu-is-open');
        $content_wrapper.toggleClass('lateral-menu-is-open');
        // firefox transitions break when parent overflow is changed, so we need to wait for the end of the trasition to give the body an overflow hidden
        $('body').toggleClass('overflow-hidden');
        $('#lateral-nav').toggleClass('lateral-menu-is-open');

        //check if transitions are not supported - i.e. in IE9
        if ($('html').hasClass('no-csstransitions')) {
            $('body').toggleClass('overflow-hidden');
        }
    });

    //close lateral menu clicking outside the menu itself
    $content_wrapper.on('click', function (event) {
        if (!$(event.target).is('#menu-trigger, #menu-trigger span')) {
            $lateral_menu_trigger.removeClass('is-clicked');
            $navigation.removeClass('lateral-menu-is-open');
            $content_wrapper.removeClass('lateral-menu-is-open').one('webkitTransitionEnd otransitionend oTransitionEnd msTransitionEnd transitionend', function () {
                $('body').removeClass('overflow-hidden');
            });
            $('#lateral-nav').removeClass('lateral-menu-is-open');
            //check if transitions are not supported
            if ($('html').hasClass('no-csstransitions')) {
                $('body').removeClass('overflow-hidden');
            }

        }
    });

    //open (or close) submenu items in the lateral menu. Close all the other open submenu items.
    $('.lateral-nav__item').children('a').click(function (event) {
        // event.preventDefault();
        $(this).toggleClass('submenu-open').next('.sub-menu').slideToggle(400).end().parent('.lateral-nav__item').siblings('.lateral-nav__item').children('a').removeClass('submenu-open').next('.sub-menu').slideUp(400);
    });

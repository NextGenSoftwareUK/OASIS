var menu_trigger = $('#menu-trigger'),
    content_wrapper = $('.main-content'),
    lateral_nav = $('#lateral-nav'),
    submenu = $('.item-has-children > a');


$(menu_trigger).on('click', function (event) {
    event.preventDefault();

    menu_span_top = $(this).children(0).children(0),
        menu_span_mid = $(this).children(0).children(1),
        menu_span_btm = $(this).children(0).children(2);

    if ($(menu_span_top).hasClass('expand-top')) {
        $(menu_span_top).removeClass('expand-top');
        $(menu_span_top).addClass('shrink-top');
    } else {
        $(menu_span_top).addClass('expand-top');
    }

    if ($(menu_span_mid).hasClass('expand-mid')) {
        $(menu_span_mid).removeClass('expand-mid');
        $(menu_span_mid).addClass('shrink-mid');
    } else {
        $(menu_span_mid).addClass('expand-mid');
    }

    if ($(menu_span_btm).hasClass('expand-btm')) {
        $(menu_span_btm).removeClass('expand-btm');
        $(menu_span_btm).addClass('shrink-btm');
    } else {
        $(menu_span_btm).addClass('expand-btm');
    }

    $(menu_trigger).toggleClass('is-clicked');
    // $header.toggleClass('lateral-menu-is-open');
    $(lateral_nav).toggleClass('lateral-menu-is-open').one('webkitTransitionEnd otransitionend oTransitionEnd msTransitionEnd transitionend', function () {
        // firefox transitions break when parent overflow is changed, so we need to wait for the end of the trasition to give the body an overflow hidden
        $('body').toggleClass('overflow-hidden');
    });
    // $(lateral_nav).toggleClass('lateral-menu-is-open');

    //check if transitions are not supported - i.e. in IE9
    if ($('html').hasClass('no-csstransitions')) {
        $('body').toggleClass('overflow-hidden');
    }

    if ($(submenu).hasClass('submenu-open')) {
        $(submenu).removeClass('submenu-open').next('.sub-menu').delay(400).slideUp(300);
    }
})




let Triggers = document.querySelectorAll('.modal-trigger');

for (let trigger of Triggers) {
    trigger.addEventListener('click', (e) => {
        const et = e.target;
        const visible = document.querySelector(visible);

        if (visible) {
            visible.classList.remove(visible);
        }
        et.classList.add(visible);

        let Modals = document.querySelectorAll('.signin-modal__container');

        for (let modal of Modals) {
            if (modal.getAttribute('id') === trigger.getAttribute('data-show')) {
                modal.style.display = "block";
            } else {
                modal.style.display = "none";
            }
        }
    });
}


// this.element.addEventListener('click', function (event) {
//     if (!event.target.hasClass('signin-modal-container')) {
//         document.getElementById("divNewAccount").style.display = "none";
//         document.getElementById("divLogin").style.display = "block";
//         document.getElementById("divForgotPassword").style.display = "none";
//         event.preventDefault();
//         self.element.removeClass('signin-modal--is-visible');
//     }
// })
// // close modal when clicking the esc keyboard button
// document.addEventListener('keydown', function (event) {
//     (event.which == '27') && removeClass(self.element, 'signin-modal--is-visible');
// });
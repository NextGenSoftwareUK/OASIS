$('#divLogin').addClass('signin-modal__container--is-visible');                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                dClass('signin-modal__container--is-visible');

$('#divNewAcount').addClass('.signin-modal__container--is-visible');

// $('#divForgotPassword').addClass('signin-modal__container--is-visible');


$(function () {

    $('body').removeClass('preload');

});


var menu_trigger = $('#menu-trigger'),
    content_wrapper = $('.main-content'),
    lateral_nav = $('#lateral-nav'),
    submenu = $('.item-has-children > a'),
    visibility = ('signin-modal__container--is-visible'),
    modalTrigger = document.querySelectorAll('.modal-trigger'),
    modals = document.getElementsByClassName('.signin-modal__container');

// $(modalTrigger).on('click', function (event) {
//     // $data = $(this).attr('href');

//     event.preventDefault();
//     if (('event.target').hasClass(visibility)) {
//         $(container).not($(this).next()).removeClass(visibility),
//             //  } else {
//             //      $(container).removeClass($(visibility))
//             $('#' + $data).addClass($(visibility));
//     }
// });



$('.loginTrigger').on('click', function (event) {
    event.preventDefault();
    $('modals').removeClass(visibility);
    $('#divLogin').addClass(visibility);
});

$('.signupTrigger').on('click', function (event) {
    event.preventDefault();
$('.modals').removeClass(visibility);
$('#divNewAccount').addClass(visibility);
});

$('.resetTrigger').on('click', function (event) {
    event.preventDefault();
$('.modals').removeClass(visibility);
$('#divForgotPassword').addClass(visibility);
});

// $('modalTrigger').on('click', function (event) {
//     data = $(this).attr('data-show');
//     container = $('.signin-modal_container');
//     visibility = ('signin-modal__container--is-visible');

//     event.preventDefault();

//     if ($(this).hasClass($(visibility))) {
//         $(container).removeClass($(visibility)),
//             $('#' + $(data)).addClass($(visibility))
//     };
// });
// var menu_trigger = $('#menu-trigger'),
//     content_wrapper = $('.main-content'),
//     lateral_nav = $('#lateral-nav'),
//     submenu = $('.item-has-children > a'),
//     visibility = ('signin-modal__container--is-visible'),
//     modalTrigger = document.querySelectorAll('a.modal-trigger');


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
    }),

   



    // $modal_trigger.on('click', function (event) {
    //     event.preventDefault();

    //     $login = $('loginTrigger'),
    //         $signup = $('signupTrigger'),
    //         $reset = $('resetTrigger');






    //open-close lateral menu clicking on the menu icon


    //close lateral menu clicking outside the menu itself
    $(content_wrapper).on('click', function (event) {
        if (!$(event.target).is('.modal-trigger')) {
            // $(container).removeClass($(visibility));
            $menu_trigger.removeClass('is-clicked');
            $(lateral_nav).removeClass('lateral-menu-is-open').one('webkitTransitionEnd otransitionend oTransitionEnd msTransitionEnd transitionend', function () {
                $('body').removeClass('overflow-hidden');
            });
            // $(lateral_nav).removeClass('lateral-menu-is-open');
            $(submenu).removeClass('submenu-open').next('.sub-menu').delay(400).slideUp(300);
            //check if transitions are not supported
            if ($('html').hasClass('no-csstransitions')) {
                $('body').removeClass('overflow-hidden');
            }

        }
    });

//open (or close) submenu items in the lateral menu. Close all the other open submenu items.
$('.item-has-children').children('a').on('click', function (event) {
    event.preventDefault();
    $(this).toggleClass('submenu-open').next('.sub-menu').slideToggle(300).end().parent('.item-has-children').siblings('.item-has-children').children('a').removeClass('submenu-open').next('.sub-menu').slideUp(300);
});
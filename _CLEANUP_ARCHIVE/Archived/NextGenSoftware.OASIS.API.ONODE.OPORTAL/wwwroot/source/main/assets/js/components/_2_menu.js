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
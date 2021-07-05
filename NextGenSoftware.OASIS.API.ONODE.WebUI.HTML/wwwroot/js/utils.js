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
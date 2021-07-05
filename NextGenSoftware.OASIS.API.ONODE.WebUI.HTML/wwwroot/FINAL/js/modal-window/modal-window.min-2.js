! function() {
    var e = function(e) {
        this.element = e, this.triggers = document.querySelectorAll('[aria-controls="' + this.element.getAttribute("id") + '"]'), this.firstFocusable = null, this.lastFocusable = null, this.moveFocusEl = null, this.modalFocus = this.element.getAttribute("data-modal-first-focus") ? this.element.querySelector(this.element.getAttribute("data-modal-first-focus")) : null, this.selectedTrigger = null, this.preventScrollEl = this.getPreventScrollEl(), this.showClass = "x-nkj", this.initModal()
    };

    function s(e) {
        return e.offsetWidth || e.offsetHeight || e.getClientRects().length
    }
    e.prototype.getPreventScrollEl = function() {
        var e = !1,
            t = this.element.getAttribute("data-modal-prevent-scroll");
        return t && (e = document.querySelector(t)), e
    }, e.prototype.initModal = function() {
        var t = this;
        if (this.triggers)
            for (var e = 0; e < this.triggers.length; e++) this.triggers[e].addEventListener("click", function(e) {
                e.preventDefault(), Util.hasClass(t.element, t.showClass) ? t.closeModal() : (t.selectedTrigger = e.target, t.showModal(), t.initModalEvents())
            });
        this.element.addEventListener("openModal", function(e) {
            e.detail && (t.selectedTrigger = e.detail), t.showModal(), t.initModalEvents()
        }), this.element.addEventListener("closeModal", function(e) {
            e.detail && (t.selectedTrigger = e.detail), t.closeModal()
        }), Util.hasClass(this.element, this.showClass) && this.initModalEvents()
    }, e.prototype.showModal = function() {
        var s = this;
        Util.addClass(this.element, this.showClass), this.getFocusableElements(), this.moveFocusEl && (this.moveFocusEl.focus(), this.element.addEventListener("transitionend", function e(t) {
            s.moveFocusEl.focus(), s.element.removeEventListener("transitionend", e)
        })), this.emitModalEvents("modalIsOpen"), this.preventScrollEl && (this.preventScrollEl.style.overflow = "hidden")
    }, e.prototype.closeModal = function() {
        Util.hasClass(this.element, this.showClass) && (Util.removeClass(this.element, this.showClass), this.firstFocusable = null, this.lastFocusable = null, this.moveFocusEl = null, this.selectedTrigger && this.selectedTrigger.focus(), this.cancelModalEvents(), this.emitModalEvents("modalIsClose"), this.preventScrollEl && (this.preventScrollEl.style.overflow = ""))
    }, e.prototype.initModalEvents = function() {
        this.element.addEventListener("keydown", this), this.element.addEventListener("click", this)
    }, e.prototype.cancelModalEvents = function() {
        this.element.removeEventListener("keydown", this), this.element.removeEventListener("click", this)
    }, e.prototype.handleEvent = function(e) {
        switch (e.type) {
            case "click":
                this.initClick(e);
            case "keydown":
                this.initKeyDown(e)
        }
    }, e.prototype.initKeyDown = function(e) {
        e.keyCode && 9 == e.keyCode || e.key && "Tab" == e.key ? this.trapFocus(e) : (e.keyCode && 13 == e.keyCode || e.key && "Enter" == e.key) && e.target.closest(".js-modal__close") && (e.preventDefault(), this.closeModal())
    }, e.prototype.initClick = function(e) {
        (e.target.closest(".js-modal__close") || Util.hasClass(e.target, "js-modal")) && (e.preventDefault(), this.closeModal())
    }, e.prototype.trapFocus = function(e) {
        this.firstFocusable == document.activeElement && e.shiftKey && (e.preventDefault(), this.lastFocusable.focus()), this.lastFocusable != document.activeElement || e.shiftKey || (e.preventDefault(), this.firstFocusable.focus())
    }, e.prototype.getFocusableElements = function() {
        var e = this.element.querySelectorAll(i);
        this.getFirstVisible(e), this.getLastVisible(e), this.getFirstFocusable()
    }, e.prototype.getFirstVisible = function(e) {
        for (var t = 0; t < e.length; t++)
            if (s(e[t])) {
                this.firstFocusable = e[t];
                break
            }
    }, e.prototype.getLastVisible = function(e) {
        for (var t = e.length - 1; 0 <= t; t--)
            if (s(e[t])) {
                this.lastFocusable = e[t];
                break
            }
    }, e.prototype.getFirstFocusable = function() {
        if (this.modalFocus && Element.prototype.matches)
            if (this.modalFocus.matches(i)) this.moveFocusEl = this.modalFocus;
            else {
                this.moveFocusEl = !1;
                for (var e = this.modalFocus.querySelectorAll(i), t = 0; t < e.length; t++)
                    if (s(e[t])) {
                        this.moveFocusEl = e[t];
                        break
                    }
                this.moveFocusEl || (this.moveFocusEl = this.firstFocusable)
            }
        else this.moveFocusEl = this.firstFocusable
    }, e.prototype.emitModalEvents = function(e) {
        var t = new CustomEvent(e, {
            detail: this.selectedTrigger
        });
        this.element.dispatchEvent(t)
    };
    var t, o = document.getElementsByClassName("js-modal"),
        i = '[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), iframe, object, embed, [tabindex]:not([tabindex="-1"]), [contenteditable], audio[controls], video[controls], summary';
    if (0 < o.length) {
        for (var l = [], n = 0; n < o.length; n++) t = n, l.push(new e(o[t]));
        window.addEventListener("keydown", function(e) {
            if (e.keyCode && 27 == e.keyCode || e.key && "escape" == e.key.toLowerCase())
                for (var t = 0; t < l.length; t++) l[t].closeModal()
        })
    }
}();
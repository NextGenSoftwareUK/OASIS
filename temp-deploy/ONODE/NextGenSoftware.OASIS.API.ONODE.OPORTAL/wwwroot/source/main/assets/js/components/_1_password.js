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
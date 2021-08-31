import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Signup extends Component {
  onSignup(event) {
    event.preventDefault();
    let email = document.getElementById('signup-email').value;
    let password = document.getElementById('signup-password').value;
    let confirmPassword = document.getElementById('confirm-signup-password').value;
    let userObject = {
      email,
      password,
      confirmPassword,
      "acceptTerms": true,
      "avatarType": "User"
    }
    const userAction = async () => {
      const response = await fetch('https://api.oasisplatform.world/api/avatar/register', {
        method: 'POST',
        body: JSON.stringify(userObject), // string or object
        headers: {
          'Content-Type': 'application/json'
        }
      });
      if (response.status === 200) {
        const myJson = await response.json(); //extract JSON from the http response
        alert(myJson.message);

        // hide the login/signup buttons
        var elementList = document.getElementsByClassName("nav-logins");
        var avatarDropdowm = document.getElementByClassName("nav-avatar-dropdowm");

        for (var i = 0; i < elementList.length; i++) {
          elementList[i].classList.add('hide-logins')
        }
        avatarDropdowm.classList.add('enabled')
        //===============================//

        window.location.reload();
      }
      else {
        const myJson = await response.json(); //extract JSON from the http response
        alert(myJson.title);
        window.location.reload();
      }

    }
    userAction();
  }

  render() {
    return (
      <form className="login-form">
        <div className="login-title">
          <h1 className="login-header">Sign Up</h1>
          <p className="login-title-text">Already have an account?
            <span onClick={this.props.change} className="link">Log In!</span>
          </p>
        </div>
        <div className="login-inputs">
          <label htmlFor="signup-email">Email</label>
          <input type="email" placeholder="name@example.com" id="signup-email" />
          <label htmlFor="signup-password">Password</label>
          <input type="password" id="signup-password" />
          <label htmlFor="confirm-signup-password">Confirm Password</label>
          <input type="password" id="confirm-signup-password" />
          <div>
            <input type="checkbox" name="accept-terms" id="accept-terms" />
            <label for="accept-terms">I have read and accept the <a href="#0" className="link">Terms of
              Service</a></label>
          </div>
          <button onClick={this.onSignup} className="login-submit">Submit</button>
        </div>
      </form>
    )
  }
}

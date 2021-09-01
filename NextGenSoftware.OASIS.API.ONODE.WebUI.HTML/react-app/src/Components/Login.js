import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Login extends Component {
  onLogin(event) {
    event.preventDefault();
    let email = document.getElementById('login-email').value;
    let password = document.getElementById('login-password').value;
    let userObject = {
      email,
      password
    }
    const userAction = async () => {
      const response = await fetch('https://api.oasisplatform.world/api/avatar/authenticate', {
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
        var avatarDropdowm = document.getElementsByClassName("nav-avatar-dropdowm");

        for (var i = 0; i < elementList.length; i++) {
          elementList[i].classList.add('hide-logins')
        }
        avatarDropdowm.classList.add('enabled')
        //===============================//

        window.location.reload();
      } else {
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
          <h1 className="login-header">Log In</h1>
          <p className="login-title-text">Don't have an account?
            <span onClick={this.props.change} className="link">Sign Up!</span>
          </p>
        </div>
        <div className="login-inputs">
          <label htmlFor="login-email">EMAIL</label>
          <input id="login-email" type="email" placeholder="name@example.com" />
          <label htmlFor="login-password">PASSWORD</label>
          <input type="password" id="login-password" />
          <label className="link">Forgot Password?</label>
          <div>
            <input type="checkbox" name="remember-login" id="remember-login" />
            <label htmlFor="remember-login">Remember Me</label>
          </div>
          <button onClick={this.onLogin} className="login-submit">Submit</button>
        </div>
      </form>
    )
  }
}

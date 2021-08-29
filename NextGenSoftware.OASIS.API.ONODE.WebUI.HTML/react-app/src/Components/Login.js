import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Login extends Component {
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
          <label htmlFor="email">EMAIL</label>
          <input type="email" placeholder="name@example.com" />
          <label htmlFor="password">PASSWORD</label>
          <input type="password" />
          <label className="link">Forgot Password?</label>
          <div>
            <input type="checkbox" name="remember-login" id="remember-login" />
            <label htmlFor="remember-login">Remember Me</label>
          </div>
          <button type="submit" className="login-submit">Submit</button>
        </div>
      </form>
    )
  }
}

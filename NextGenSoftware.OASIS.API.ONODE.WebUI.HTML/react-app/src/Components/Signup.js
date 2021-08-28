import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Signup extends Component {
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
          <label htmlFor="email">Email</label>
          <input type="email" placeholder="name@example.com" />
          <label htmlFor="password">Password</label>
          <input type="password" />
          <label htmlFor="password">Confirm Password</label>
          <input type="password" />
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

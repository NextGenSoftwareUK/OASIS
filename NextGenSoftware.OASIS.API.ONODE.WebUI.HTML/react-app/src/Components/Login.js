import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Login extends Component {
  render() {
    return (
      <div className="login">
        <form className="login-form">
          <div className="login-title">
            <h1 className="login-header">Log In</h1>
            <p className="login-title-text">Don't have an account? <span className="login-title-link">Sign Up!</span></p>
          </div>
        </form>
      </div>
    )
  }
}

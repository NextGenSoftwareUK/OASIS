import { Component } from "react"
import logo from '../img/dummy-logo.svg'
import '../CSS/Navbar.css'
import SideNav from "./SideNav"
import Login from './Login'
import Signup from "./Signup"

class Navbar extends Component {
  constructor(props) {
    super(props)

    this.showSignup = this.showSignup.bind(this)
    this.showLogin = this.showLogin.bind(this)
    this.hideLogins = this.hideLogins.bind(this)
    this.state = {
      showSideNav: false,
      showLogin: false,
      showSignup: false
    }
  }

  hideLogins() {
    this.setState({
      showLogin: false,
      showSignup: false
    })
  }
  showLogin() {
    this.setState({
      showLogin: true,
      showSignup: false
    })
  }
  showSignup() {
    this.setState({
      showLogin: false,
      showSignup: true
    })
  }

  render() {
    return (
      <nav className="nav">
        <div className="nav-left">
          <div className={`nav-menu-btn${this.state.showSideNav ? " nav-menu-open" : ""}`}
            onClick={() => this.setState({ showSideNav: !this.state.showSideNav })}>
            <div className="nav-menu-btn-burger"></div>
          </div>
          <img className="nav-logo" src={logo} alt="logo" />
        </div>
        <div className="nav-right">
          <ul className="nav-list">
            <li className="nav-login"><div className="link-inverse" onClick={this.showLogin}>Log in</div> </li>
            <li className="nav-login"><div className="link-inverse" onClick={this.showSignup}>Sign up</div></li>
          </ul>
          <div className="">
            <div className="link-inverse">
              <svg className="nav-avatar" viewBox="0 0 26.5 26.5">
                <path
                  d="M24.75 13.25a11.5 11.5 0 01-11.5 11.5 11.5 11.5 0 01-11.5-11.5 11.5 11.5 0 0111.5-11.5 11.5 11.5 0 0111.5 11.5zm-3.501 8.243c-.5-3.246-4-6.246-7.995-6.238C9.25 15.247 5.75 18.247 5.25 21.5m13-11.248a5 5 0 01-5 5 5 5 0 01-5-5 5 5 0 015-5 5 5 0 015 5z"
                  fill="none" stroke="currentColor" strokeWidth="1.5" />
              </svg>
            </div>
            {/* <ul className="nav__sub-list">
              <li className="nav__sub-item"><a href="#0">My Account</a></li>
              <li className="nav__sub-item"><a href="#0">Edit Account</a></li>
              <li className="nav__sub-item"><a href="#0">Logout</a></li>
            </ul> */}
          </div>
        </div>
        <div className={`login ${this.state.showLogin || this.state.showSignup ? "show" : ""}`}>
          <button className="login-hide-btn" onClick={this.hideLogins}>X</button>
          {this.state.showLogin ? <Login change={this.showSignup} /> : null}
          {this.state.showSignup ? <Signup change={this.showLogin} /> : null}
        </div>
        <SideNav show={this.state.showSideNav} />
      </nav>
    );
  }
}

export default Navbar;
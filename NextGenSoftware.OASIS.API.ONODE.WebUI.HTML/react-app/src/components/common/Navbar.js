import React from "react";

import logo from "../../assets/images/dummy-logo.svg";
import loginIcon from "../../assets/images/loggedin.png";

class Navbar extends React.Component {

    handleLogoClicked = (showLogin) => {
        let user = localStorage.getItem('user');
        if(user === 'undefined' || !user) {
            showLogin();
        }
    }

    render() {
        const { user, showLogin, showSignup, handleLogout, showSidebar, toggleSidebar } = this.props;

        return (
            <nav className="nav">
                <div className="nav-left">
                    <div 
                        className={`nav-menu-btn ${showSidebar ? "nav-menu-open" : ""}`} 
                        onClick={toggleSidebar}
                    >
                        <div className="nav-menu-btn-burger"></div>
                    </div>

                    <a className="cursor-pointer" onClick={() => this.handleLogoClicked(showLogin)}>
                        <img className="nav-logo" src={logo} alt="logo" />
                    </a>
                </div>

                <div className="nav-right">
                    {
                        user ? null :
                        <ul>
                            <li onClick={showLogin}><a href="#">Log in</a> </li>
                            <li onClick={showSignup}><a href="#">Sign up</a></li>
                        </ul>
                    }

                    <ul>
                        <li className="have-avatar">
                            <a href="#">
                                {
                                    user ? <img src={loginIcon} alt="icon" /> :
                                    <svg viewBox="0 0 26.5 26.5">
                                        <path
                                            d="M24.75 13.25a11.5 11.5 0 01-11.5 11.5 11.5 11.5 0 01-11.5-11.5 11.5 11.5 0 0111.5-11.5 11.5 11.5 0 0111.5 11.5zm-3.501 8.243c-.5-3.246-4-6.246-7.995-6.238C9.25 15.247 5.75 18.247 5.25 21.5m13-11.248a5 5 0 01-5 5 5 5 0 01-5-5 5 5 0 015-5 5 5 0 015 5z"
                                            fill="none" 
                                            stroke="currentColor" 
                                            strokeWidth="1.5" 
                                        />
                                    </svg>
                                }    
                            </a>

                            {
                                user ?
                                
                                <ul className="inner-menu">
                                    <li><a href="#">My Account</a></li>
                                    <li><a href="#">Edit Account</a></li>
                                    <li><a href="#" onClick={handleLogout}>Logout</a></li>
                                </ul>

                                : null
                            }
                        </li>
                    </ul>
                </div>
            </nav>
        );
    }
}

export default Navbar;
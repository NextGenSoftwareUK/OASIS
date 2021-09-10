import React from 'react';
import data from './SideNavData'
import SideMenu from './SideMenu';
import '../CSS/SideNav.css';

export class SideNav extends React.Component {
    constructor(props) {
        super(props)

        this.state = {
            showSubNav: false
        }
    }

    render() {
        return (
            <div className={`side-nav${this.props.show ? " side-nav-show" : ""}`}>
                <ul className="side-nav-list">
                    {data.map((menu, index) => <SideMenu menu={menu} key={index} hideSideNav={this.props.hideSideNav} />)}
                    <li className="side-nav-logins"><div className="link-inverse" onClick={this.props.showLogin}>Log in</div> </li>
                    <li className="side-nav-logins"><div className="link-inverse" onClick={this.props.showSignup}>Sign up</div></li>
                </ul>
            </div>
        )
    }
}

export default SideNav;

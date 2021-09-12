import React, { Component } from 'react'
import { Link } from 'react-router-dom';
import SlideToggle from 'react-slide-toggle'

export default class SideMenu extends Component {
  render() {
    const menu = this.props.menu
    return (
      <SlideToggle
        collapsed
        duration={300}
        render={({ onToggle, setCollapsibleElement }) => (
          <div>
            <div onClick={() => {

              onToggle();
            }}
              className="side-nav-menu link">{menu.title}</div>
            <ul className="side-nav-submenu" ref={setCollapsibleElement}>
              {menu.subNav.map((item, index) =>
                <li key={index} onClick={this.props.hideSideNav}>
                  <Link className="side-nav-subnav link" to={item.path}>{item.title}</Link>
                </li>)}
            </ul>
          </div>
        )}
      />
    )
  }
}
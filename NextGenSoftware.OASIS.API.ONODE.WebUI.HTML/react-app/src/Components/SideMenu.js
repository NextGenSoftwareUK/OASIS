import React, { Component } from 'react'
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
            <div onClick={onToggle} className="side-nav-menu">{menu.title}</div>
            <div className="side-nav-submenu" ref={setCollapsibleElement}>
              {menu.subNav.map((title, index) => <div className="side-nav-subnav" key={index}>{title}</div>)}
            </div>
          </div>
        )}
      />
    )
  }
}
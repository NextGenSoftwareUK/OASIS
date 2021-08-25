import React, { Component } from 'react'
import '../CSS/SideNav.css'

const data = [{
  title: "the oasis",
  subNav: ["about", "documentation", "our world"]
}, {
  title: "avatar",
  subNav: ["view avatar", "edit avatar", "search avatars"]
}, {
  title: "karma",
  subNav: []
}, {
  title: "data",
  subNav: []
}, {
  title: "seeds",
  subNav: []
}, {
  title: "provider",
  subNav: []
}, {
  title: "nft",
  subNav: []
}, {
  title: "map",
  subNav: []
}, {
  title: "oapp",
  subNav: []
}, {
  title: "quest",
  subNav: []
}, {
  title: "mission",
  subNav: []
}, {
  title: "egg",
  subNav: []
}, {
  title: "game",
  subNav: []
}, {
  title: "developer",
  subNav: []
}]

export class SideNav extends Component {
  render() {
    return (
      <div className="side-nav">
        <ul>
          {data.map((link) => <li>{link.title}</li>)}
        </ul>
      </div>
    )
  }
}

export default SideNav

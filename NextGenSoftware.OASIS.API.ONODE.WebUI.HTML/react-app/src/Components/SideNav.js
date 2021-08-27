import React, { Component } from 'react'
import '../CSS/SideNav.css'
import SideMenu from './SideMenu'

const data = [{
  title: "the oasis",
  subNav: ["about", "documentation", "our world"]
}, {
  title: "avatar",
  subNav: ["view avatar", "edit avatar", "search avatars"]
}, {
  title: "karma",
  subNav: [
    "View Curent Karma Weightings",
    "Vote For Karma Weightings",
    "View Avatar Karma",
    "View/Search Karma Akashic Records"
  ]
}, {
  title: "data",
  subNav: [
    "Load Data",
    "Send Data",
    "Manage Data",
    "Cross-Chain Management",
    "Off-Chain Management",
    "Search Data"

  ]
}, {
  title: "seeds",
  subNav: [
    "Pay With SEEDS",
    "Donate SEEDS",
    "Reward SEEDS",
    "Invite To Join SEEDS",
    "Accept Invit to join seeds",
    "View SEEDS",
    "View Organisations",
    "Manage SEEDS",
    "Search Seeds"
  ]
}, {
  title: "provider",
  subNav: [
    "View Providers",
    "Manage Providers",
    "Manage Auto-Replication",
    "Manage Auto-Fail-Over",
    "Manage Load Balancing",
    "View Provider Stats",
    "Compare Provider Speeds",
    "Search Providers",
    "Holochain",
    "SEEDS",
    "EOSIO",
    "Ethereum",
    "IPFS",
    "ThreeFold",
    "SOLID",
    "Activity Pub",
    "Mongo DB",
    "SQLLite",
    "Neo4j"
  ]
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
  subNav: [
    "View StarCraft 2 Leagues",
    "View StarCraft 2 Tournaments",
    "View StarCraft 2 Achievements"]
}, {
  title: "developer",
  subNav: []
}]

export class SideNav extends Component {
  constructor(props) {
    super(props)

    this.state = {
      showSubNav: false
    }
  }

  render() {
    return (
      <div className={`side-nav${this.props.show ? " side-nav-show" : ""}`}>
        <ul>
          {data.map((menu, index) => <SideMenu menu={menu} key={index} />)}
        </ul>
      </div>
    )
  }
}

export default SideNav

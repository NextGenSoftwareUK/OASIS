import React, { Component } from 'react'
import '../CSS/SideNav.css'
import SideMenu from './SideMenu'
import { DescriptionTwoTone } from '@material-ui/icons';

const data = [{
  title: "the oasis",
  subNav: ["about", "documentation", "our world"]
}, {
  title: "avatar",
  subNav: ["avatar", "edit avatar", "search avatars"]
}, {
  title: "karma",
  subNav: [
    "Curent Karma Weightings",
    "Vote For Karma Weightings",
    "Avatar Karma",
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
    "SEEDS",
    "Organisations",
    "Manage SEEDS",
    "Search Seeds"
  ]
}, {
  title: "provider",
  subNav: [
    "Providers",
    "Manage Providers",
    "Manage Auto-Replication",
    "Manage Auto-Fail-Over",
    "Manage Load Balancing",
    "Provider Stats",
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
  subNav: [
    "OASIS NFTs",
    "Manage OASIS NFTs",
    "Search OASIS NFTs",
    "Purchase OASIS NFT",
    "Purchase OASIS Virtual Land NFT" 
  ]
}, {
  title: "map",
  subNav: [
    "Global 3D Map",
    "Mange Map",
    "Add Quest To Map",
    "Add 2D Object To Map",
    "Add 3D Object To Map",
    "Plot Route On Map",
    "View OAPP's On Map",
    "View Holons On Map",
    "View Quests On Map",
    "Search Map",
    "Download Our World",
  ]
}, {
  title: "oapp",
  subNav: [
    "Installed OAPPs",
    "Manage OAPPs",
    "Create OAPP",
    "Deploy OAPP",
    "Edit OAPP",
    "Launch OAPP",
    "Search OAPPs",
    "Download Our World"
  ]
}, {
  title: "quest",
  subNav: [
    "View Quests",
    "Manage Quests",
    "Search Quests"
  ]
}, {
  title: "mission",
  subNav: [
    "View Missions",
    "Manage Missions",
    "Search Missions"
  ]
}, {
  title: "egg",
  subNav: [
    "View Eggs",
    "Manage Eggs",
    "Search Eggs"
  ]
}, {
  title: "game",
  subNav: [
    "StarCraft 2 Leagues",
    "StarCraft 2 Tournaments",
    "StarCraft 2 Achievements"]
}, {
  title: "developer",
  subNav: [
    "OASIS API",
    "LIVE",
    "STAGING",
    "UI LIVE",
    "UI STAGING",
    "STAR ODK",
    <React.Fragment>
    <DescriptionTwoTone id="material-icons">description</DescriptionTwoTone><text>Postman <span class="jsonIcon">JSON</span></text>
    </React.Fragment>,
    <React.Fragment>
    <DescriptionTwoTone id="material-icons">description</DescriptionTwoTone><text>DEV ENVIROMENT Postman <span class="jsonIcon">JSON</span></text>
    </React.Fragment>,
    <React.Fragment>
    <DescriptionTwoTone id="material-icons">description</DescriptionTwoTone><text>STAGING
                        ENVIROMENT Postman <span class="jsonIcon">JSON</span></text>
    </React.Fragment>,
    <React.Fragment>
    <DescriptionTwoTone id="material-icons">description</DescriptionTwoTone><text>LIVE ENVIROMENT Postman <span class="jsonIcon">JSON</span></text>
    </React.Fragment>,
    "Code/Documentation",
    "The Justice League Academy"
  ]
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
        <ul className="side-nav-list">
          {data.map((menu, index) => <SideMenu menu={menu} key={index} />)}
          <li className="side-nav-logins"><div className="link-inverse" onClick={this.props.showLogin}>Log in</div> </li>
          <li className="side-nav-logins"><div className="link-inverse" onClick={this.props.showSignup}>Sign up</div></li>
        </ul>
      </div>
    )
  }
}

export default SideNav

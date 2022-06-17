import React from "react";

import Navbar from "./common/Navbar";
import Sidebar from "./common/sidebar/Sidebar";
import Login from "./Login";
import Signup from "./Signup";
import Solana from "./popups/nft/Solana";
import ContactPopup from "./popups/nft/ContactPopup";
import Karma from "./popups/karma";

import "../assets/scss/general.scss";
import "../assets/scss/style.scss";

import axios from "axios";
import "react-toastify/dist/ReactToastify.css";
import Avatar from "./popups/avatar";
import DataScreen from "./popups/data-screen";
import Seeds from "./popups/seeds";
import ForgotPassword from "./forgotPassword";
import ComingSoon from "./popups/comingsoon/ComingSoon";

class App extends React.Component {
  state = {
    showSidebar: false,
    showLogin: false,
    showSignup: false,
    showForgetPassword: false,
    user: null,
    sidebarMenuOption: [
      {
        data: {
          loadData: false,
          sendData: false,
          manageData: false,
          offChainManagement: false,
          crossChainManagement: false,
        },
      },
      {
        nft: {
          solana: false,
          contactPopup: false,
        },
      },
      {
        seeds: {
          acceptInvite: false,
          payWithSeeds: false,
          donateSeeds: false,
          sendInvite: false,
          rewardSeeds: false,
        },
      },
      {
        avatar: {
          avatarWallet: false,
          viewAvatar: false,
        },
      },
      {
        karma: {
          viewKarma: false,
        },
      },
      {
        comingSoon: false,
      }
    ],
  };

  componentDidMount() {
    let user = localStorage.getItem('user');
    if(user === 'undefined' || !user) {
      this.setState({ user: localStorage.getItem("user") });
    }
}

    setUserData = (data) => {
        this.setState({
            user: data,
        });
    };

    toggleSidebar = () => {
        this.setState({
            showSidebar: !this.state.showSidebar,
        });
    };

    hidePopups = () => {
        this.setState({
            showLogin: false,
            showSignup: false,
            showForgetPassword: false
        });
    };

    hideLogin = () => {
        this.setState({
            showLogin: false,
        });
    };

    hideSignup = () => {
        this.setState({
            showSignup: false,
        });
    };

    showLogin = () => {
        console.log('going to call login')
        this.setState({
            showLogin: true,
            showSignup: false,
            showForgetPassword: false
        });
    };

    showSignup = () => {
        this.setState({
            showSignup: true,
            showLogin: false,
            showForgetPassword: false
        });
    };

    handleLogout = () => {
        axios
            .post("https://api.oasisplatform.world/api/avatar/revoke-token", {
                token: this.state.user.jwtToken,
            })
            .then((res) => {
                this.setState({ user: null });
                localStorage.removeItem("user");
                localStorage.removeItem("credentials");
            })
            .catch((err) => {
                this.setState({ user: null });
                localStorage.removeItem("user");
                localStorage.removeItem("credentials");
            });
    };

    toggleScreenPopup = (menuOption, menuName) => {
        console.log(menuOption);
        console.log(menuName)
        let sidebarMenuOption = [...this.state.sidebarMenuOption];
        if (!menuName) {
            sidebarMenuOption[5].comingSoon = !sidebarMenuOption[5].comingSoon
        } else {
            sidebarMenuOption.map((item) => {
                if (item[menuOption]) {
                    item[menuOption][menuName] = !item[menuOption][menuName];
                }
            })
        }

        this.setState({
            sidebarMenuOption
        })
    };

    render() {
        return (
            <div className="main-container">
                <header>
                    <Navbar
                        showSidebar={this.state.showSidebar}
                        toggleSidebar={this.toggleSidebar}
                        showLogin={this.showLogin}
                        showSignup={this.showSignup}
                        handleLogout={this.handleLogout}
                        user={this.state.user}
                    />
                    
                    <Sidebar
                        showSidebar={this.state.showSidebar}
                        toggleSidebar={this.toggleSidebar}
                        toggleScreenPopup={this.toggleScreenPopup}
                    />
                </header>

                <Login
                    className="custom-form"
                    show={this.state.showLogin}
                    hide={this.hideLogin}
                    change={this.showSignup}
                    setUserStateData={this.setUserData}
                />

                <Signup
                    className="custom-form"
                    show={this.state.showSignup}
                    hide={this.hideSignup}
                    change={this.showLogin}
                />

                <DataScreen
                    data={this.state.sidebarMenuOption[0].data}
                    toggleScreenPopup={this.toggleScreenPopup}
                />

                <Solana
                    show={this.state.sidebarMenuOption[1].nft.solana}
                    hide={this.toggleScreenPopup}
                />

                <ContactPopup
                    show={this.state.sidebarMenuOption[1].nft.contactPopup}
                    hide={this.toggleScreenPopup}
                />
                <Seeds
                    seeds={this.state.sidebarMenuOption[2].seeds}
                    toggleScreenPopup={this.toggleScreenPopup}
                />
                <Avatar
                    avatar={this.state.sidebarMenuOption[3].avatar}
                    toggleScreenPopup={this.toggleScreenPopup}
                />

                <Karma 
                    karma={this.state.sidebarMenuOption[4].karma}
                    toggleScreenPopup={this.toggleScreenPopup}
                />

                <ComingSoon
                    show={this.state.sidebarMenuOption[5].comingSoon}
                    toggleScreenPopup={this.toggleScreenPopup}
                />
            </div>
        );
    }

}

export default App;

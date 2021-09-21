import React from "react";
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";

import SideNav from "./common/SideNav";
import Navbar from "./common/Navbar";
import PayWithSeeds from "./pages/seeds/PayWithSeeds";
import SendInvite from "./pages/seeds/SendInvite";
import Karma from "./pages/karma/Karma";
import Home from "./pages/Home";
import Login from "./Login";
import Signup from "./Signup";
import AddData from "./pages/data-screen/AddData";
import ShowAllData from "./pages/data-screen/ShowAllData";


import "../assets/scss/general.scss";
import "../assets/scss/style.scss";
import "../assets/scss/seeds.scss";

import axios from "axios";
import AcceptInvite from "./pages/seeds/AcceptInvite";
import SearchSeeds from "./pages/seeds/SearchSeeds";
import ViewSeeds from "./pages/seeds/ViewSeeds";

class App extends React.Component {
    state = {
        showSidebar: false,
        showLogin: false,
        showSignup: false,
        user: null
    }

    componentDidMount() {
        localStorage.getItem('user')

        if (localStorage.getItem('user')) {
            this.setState({ user: JSON.parse(localStorage.getItem('user')) })
        }
    }

    setUserData = (data) => {
        console.log(data);
        this.setState({
            user: data
        })
    }

    toggleSidebar = () => {
        this.setState({
            showSidebar: !this.state.showSidebar
        })
    }

    hidePopups = () => {
        this.setState({
            showLogin: false,
            showSignup: false
        })
    }

    hideLogin = () => {
        this.setState({
            showLogin: false,
        })
    }

    hideSignup = () => {
        this.setState({
            showSignup: false,
        })
    }

    showLogin = () => {
        this.setState({
            showLogin: true,
            showSignup: false,
        })
    }

    showSignup = () => {
        this.setState({
            showSignup: true,
            showLogin: false
        })
    }

    handleLogout = () => {
        axios.post('https://api.oasisplatform.world/api/avatar/revoke-token', {
            token: this.state.user.jwtToken
        }).then(res => {
            this.setState({ user: null })
            localStorage.removeItem('user')
            localStorage.removeItem('credentials')
        }).catch(err => {
            this.setState({ user: null })
            localStorage.removeItem('user')
            localStorage.removeItem('credentials')
        })
    }

    render() {
        return (
            <div className="main-container">
                <Router>
                    <header>
                        <Navbar
                            showSidebar={this.state.showSidebar}
                            toggleSidebar={this.toggleSidebar}
                            showLogin={this.showLogin}
                            showSignup={this.showSignup}
                            handleLogout={this.handleLogout}
                            user={this.state.user}
                        />
                        <SideNav
                            showSidebar={this.state.showSidebar}
                            toggleSidebar={this.toggleSidebar}
                        />
                    </header>

                    <div className="content-container">
                        <Switch>
                            <Route exact path="/home" component={Home} />

                            <Route exact path="/add-data" component={AddData} />
                            <Route exact path="/show-data" component={ShowAllData} />

                            {/* ============ Seeds Routes ====================== */}
                            <Route path="/pay-with-seeds" component={PayWithSeeds} />
                            <Route path="/donate-with-seeds">
                                <PayWithSeeds seedType="Donate" />
                            </Route>
                            <Route path="/reward-with-seeds">
                                <PayWithSeeds seedType="Reward" />
                            </Route>
                            <Route path="/accept-invite-to-join-seeds" component={AcceptInvite} />
                            <Route path="/send-invite-to-join-seeds" component={SendInvite} />
                            <Route exact path="/search-seeds" component={SearchSeeds} />
                            <Route exact path="/view-seeds" component={ViewSeeds} />
                            {/* ================================================ */}

                            <Route exact path="/karma" component={Karma} />
                        </Switch>
                    </div>
                </Router>

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
            </div>
        );
    }
}

export default App;

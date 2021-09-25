import axios from "axios";
import { Component } from "react";
import "../../../assets/scss/karma.scss";
import { extractKarma, login } from "../../../functions";
import ReactGrid from "../../ReactGrid";
import Loader from "react-loader-spinner";

class Karma extends Component {
    constructor(props) {
        super(props);
        this.state = {
            columns: [
                { name: "date", title: "Date" },
                { name: "avatar", title: "Avatar" },
                { name: "posNeg", title: "Positive/Negative" },
                { name: "type", title: "Type" },
                { name: "karma", title: "Karma" },
                { name: "source", title: "Source" },
                { name: "title", title: "Title" },
                { name: "description", title: "Description" },
                { name: "link", title: "Weblink" },
            ],
            rows: [],
            loading: true,
            loggedIn: true,
        };
    }

    //run this after component mounts

    async componentDidMount() {
        let token, refresh, credentials;

        //If user object exists in localstorage, get the refresh token
        //and the jwtToken
        if (localStorage.getItem("user")) {
            credentials = JSON.parse(localStorage.getItem("credentials"));
            let avatar = await login(credentials);
            if (avatar !== -1) {
                token = avatar.jwtToken;
                refresh = avatar.refreshToken;
            }
        }

        //else (for now) show an alert and redirect to home
        else {
            // alert("not logged in");
            this.setState({ loggedIn: false });
        }
        let config = {
            method: "get",
            url: "https://api.oasisplatform.world/api/avatar/GetAll",
            headers: {
                Authorization: `Bearer ${token}`,
                Cookie: `refreshToken=${refresh}`,
            },
        };
        this.setState({ loading: true });
        axios(config)
            .then(async (response) => {
                let karmaRecord = await extractKarma(response.data, {
                    jwt: token,
                    refresh,
                });
                this.setState({ rows: karmaRecord });
                console.log(karmaRecord);
                this.setState({ loading: false });
                this.setState({ loggedIn: true });
            })
            .catch((error) => {
                this.setState({ loading: true });
                // console.log(error.response);
            });
    }

    render() {
        return (
            <div className="karma">
                {this.state.loggedIn ? (
                    <div className="karma__body">
                        {this.state.loading ? (
                            <Loader type="Oval" height={30} width={30} color="#fff" />
                        ) : (
                            <ReactGrid
                                rows={this.state.rows}
                                columns={this.state.columns}
                            />
                        )}
                    </div>
                ) : (
                    <h1>You are not logged in! </h1>
                )}
            </div>
        );
    }
}
export default Karma;

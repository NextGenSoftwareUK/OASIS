import axios from "axios";
import { Component } from "react";
import "../CSS/Karma.css";
import { extractKarma } from "../functions";
import ReactGrid from "../Components/ReactGrid";

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
        };
    }

    //run this after component mounts

    componentDidMount() {
        let token, refresh;

        //If user object exists in localstorage, get the refresh token
        //and the jwtToken
        if (localStorage.getItem("user")) {
            token = JSON.parse(localStorage.getItem("user")).jwtToken;
            refresh = JSON.parse(localStorage.getItem("user")).refreshToken;
        }

        //else (for now) show an alert and redirect to home
        else {
            alert("not logged in");
            this.props.history.push("/");
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
            })
            .catch((error) => {
                this.setState({ loading: true });
                // console.log(error.response);
            });
    }

    render() {
        return (
            <div className="karma">
                <div className="karma__body">
                    {this.state.loading ? (
                        <p>Loading</p>
                    ) : (
                        <ReactGrid
                            rows={this.state.rows}
                            columns={this.state.columns}
                        />
                    )}
                </div>
            </div>
        );
    }
}
export default Karma;

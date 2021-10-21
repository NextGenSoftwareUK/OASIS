import React from "react"
import { Link } from "react-router-dom"
import axios from "axios"
import Loader from "react-loader-spinner"
import { Modal, ModalBody } from "react-bootstrap"
import { login, getUserById } from "../../../functions"
import ReactGrid from "../../ReactGrid"

import "../../../assets/scss/avatar-popup.scss"
// import "../../../assets/scss/popup.scss"

class ViewAvatar extends React.Component {
    constructor(props) {
        super(props)
        this.state = {
            columns: [
                { name: 'avatar', title: 'Avatar' },
                { name: 'level', title: 'Level' },
                { name: 'karma', title: 'Karma' },
                { name: 'sex', title: 'Sex' },
                { name: 'created', title: 'Created' },
                { name: 'modified', title: 'Last Beamed In' },
                { name: 'online', title: 'Online' },
            ],
            columnWidth: [
                { columnName: 'avatar', width: 220 },
                { columnName: 'level', width: 90 },
                { columnName: 'karma', width: 90 },
                { columnName: 'sex', width: 90 },
                { columnName: 'created', width: 150 },
                { columnName: 'modified', width: 150 },
                { columnName: 'online', width: 90 }
            ],
            rows: [],
            loading: true,
            loggedIn: true
        }
    }

    async componentDidMount() {
        var token, refresh, credentials;

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
                let avatars = []
                for (let i = 0; i <= response.data.length - 1; i++) {
                    const data = response.data[i]
                    const id = data.id;
                    let tkn = { jwt: token }
                    const user = await getUserById(id, tkn)
                    console.log(user)
                    const avatar = {
                        avatar: data.username,
                        level: data.level,
                        karma: data.karma,
                        sex: 'Male',
                        created: user.createdDate,
                        modified: user.modifiedDate,
                        online: data.isBeamedIn ? 'Yes' : 'No'
                    }
                    avatars.push(avatar)
                }

                this.setState({ rows: avatars });
                // console.log(avatars);
                this.setState({ loading: false });
                this.setState({ loggedIn: true });
            })
            .catch((error) => {
                this.setState({ loading: true });
                // console.log(error.response);
            });
    }

    render() {
        const { show, hide } = this.props;
        return (
            <Modal
                centered
                className="custom-modal custom-popup-component"
                size="xl"
                show={show}
                onHide={() => hide('avatar', 'viewAvatar')}
            >
               
                <ModalBody
					className="p-50">
                    <span className="form-cross-icon" onClick={() => hide('avatar', 'viewAvatar')}>
                        <i className="fa fa-times"></i>
                    </span>
                    {/* {this.state.loggedIn ? ( */}
                        <>
                            {/* {this.state.loading ? (
                                <Loader type="Oval" height={30} width={30} color="#fff" />
                            ) : */}
                                <ReactGrid
                                    rows={this.state.rows}
                                    columns={this.state.columns}
                                    columnWidths={this.state.columnWidth}
                                />
                            {/* } */}
                        </>
                    {/* ) : (
                        <h2 >You are not logged in! </h2>
                    )} */}

                </ModalBody>

            </Modal>
        )
    }
}

export default ViewAvatar

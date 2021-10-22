import React from "react";
import axios from "axios";
import { Modal, ModalBody } from "react-bootstrap";

import { login, getUserById } from "../../../../functions";
// import ReactGrid from "../../../ReactGrid";
import { AgGridColumn, AgGridReact } from 'ag-grid-react'
import '../../../../assets/scss/avatar-popup.scss';

class ViewAvatar extends React.Component {
    state = {
        columnDefs: [
            { 
                field: 'Avatar',
                // filterParams: filterParams,
            },
            {
                field: 'Level',

            },
            { 
                field: 'Karma'
            },
            {
                field: 'Sex',
            },
            {
                field: 'Created' 
            },
            
            {
                field: 'Last Beamed In' 
            },
            
            {
                field: 'Online' 
            },
        ],
        defaultColDef: {
            flex: 1,
            minWidth: 150,
            filter: true,
            sortable: true,
            floatingFilter: true,
            resizable: true,
        },
        rowData: null,
		loading: true,
		loggedIn: true,
    };

    onGridReady = (params) => {
        this.gridApi = params.api;
        this.gridColumnApi = params.columnApi;
    
        const updateData = (data) => {
          this.setState({ rowData: data });
        };
        fetch('https://www.ag-grid.com/example-assets/olympic-winners.json')
          .then((resp) => resp.json())
          .then((data) => updateData(data));
    }
    // async componentDidMount() {
    //     var token, refresh, credentials;

    //     //If user object exists in localstorage, get the refresh token
    //     //and the jwtToken
    //     if (localStorage.getItem("user")) {
    //         credentials = JSON.parse(localStorage.getItem("credentials"));
    //         let avatar = await login(credentials);
    //         if (avatar !== -1) {
    //             token = avatar.jwtToken;
    //             refresh = avatar.refreshToken;
    //         }
    //     }

    //     //else (for now) show an alert and redirect to home
    //     else {
    //         // alert("not logged in");
    //         this.setState({ loggedIn: false });
    //     }
    //     let config = {
    //         method: "get",
    //         url: "https://api.oasisplatform.world/api/avatar/GetAll",
    //         headers: {
    //             Authorization: `Bearer ${token}`,
    //             Cookie: `refreshToken=${refresh}`,
    //         },
    //     };
    //     this.setState({ loading: true });
    //     axios(config)
    //         .then(async (response) => {
    //             let avatars = []
    //             for (let i = 0; i <= response.data.length - 1; i++) {
    //                 const data = response.data[i]
    //                 const id = data.id;
    //                 let tkn = { jwt: token }
    //                 const user = await getUserById(id, tkn)
    //                 console.log(user)
    //                 const avatar = {
    //                     avatar: data.username,
    //                     level: data.level,
    //                     karma: data.karma,
    //                     sex: 'Male',
    //                     created: user.createdDate,
    //                     modified: user.modifiedDate,
    //                     online: data.isBeamedIn ? 'Yes' : 'No'
    //                 }
    //                 avatars.push(avatar)
    //             }

    //             this.setState({ rows: avatars });
    //             // console.log(avatars);
    //             this.setState({ loading: false });
    //             this.setState({ loggedIn: true });
    //         })
    //         .catch((error) => {
    //             this.setState({ loading: true });
    //             // console.log(error.response);
    //         });
    // }

    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component light-custom-popup" 
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('avatar', 'viewAvatar')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('avatar', 'viewAvatar')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <h2 className="grid-heading">View Avatar</h2>

                        <div className="ag-theme-alpine custom-ag-parent">
                            <AgGridReact
                                columnDefs={this.state.columnDefs}
                                defaultColDef={this.state.defaultColDef}
                                onGridReady={this.onGridReady}
                                rowData={this.state.rowData}
                            />
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}

export default ViewAvatar

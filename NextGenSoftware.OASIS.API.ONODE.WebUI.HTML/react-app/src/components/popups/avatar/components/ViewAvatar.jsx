import React from "react";
// import axios from "axios";
import { Modal } from "react-bootstrap";

// import { login, getUserById } from "../../../../functions";
// import ReactGrid from "../../../ReactGrid";
import { AgGridReact } from 'ag-grid-react'
import '../../../../assets/scss/avatar-popup.scss';
import oasisApi from "oasis-api";

class ViewAvatar extends React.Component {
    state = {
        columnDefs: [
            { 
                field: 'avatar',
                // filterParams: filterParams,
            },
            {
                field: 'level',

            },
            { 
                field: 'karma'
            },
            {
                field: 'sex',
            },
            {
                field: 'created' 
            },
            
            {
                field: 'last' 
            },
            
            {
                field: 'online' 
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

    onGridReady = async (params) => {
        this.gridApi = params.api;
        this.gridColumnApi = params.columnApi;

        const avatar = new oasisApi.Avatar()
        const karma = new oasisApi.Karma()

        const res = await avatar.getAll()
        if(!res.error){
            let avatars=[]
            const users = res.data.result
            for(let i=0; i<=users.length-1; i++){
                let user = users[i]
                console.log('user')
                const karmaRes = await karma.getKarmaForAvatar(user.avatarId)
                console.log(karmaRes)
                let temp = {
                    avatar: user.username,
                    level: 1,
                    karma: karmaRes.data.result,
                    sex: user.title === 'Mr' ? 'Male':'Female',
                    created: 'Now',
                    last: 'Now ',
                    online: user.isBeamedIn
                }
                avatars.push(temp)
            }
            this.setState({rowData: avatars})
        }
    }

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

                        <h1 className="single-heading">View Avatar</h1>

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

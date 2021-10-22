import React from "react"
import Loader from "react-loader-spinner";
import { Modal } from "react-bootstrap"

// import ReactGrid from "../../../ReactGrid";
import { AgGridColumn, AgGridReact } from 'ag-grid-react'
import { login } from "../../../../functions"
import '../../../../assets/scss/avatar-popup.scss';

class AvatarWallet extends React.Component {
	
    state = {
        columnDefs: [
            { 
                field: 'Date',
				filter: 'agDateColumnFilter',
                filterParams: filterParams,
            },
            {
                field: 'Note',

            },
            { field: 'Balance' },
            {
                field: 'Provider',
            },
            { field: 'Type' },
         
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
    };
	// async componentDidMount() {
	// 	// Api calls
	// 	var token, refresh, credentials;

	// 	//If user object exists in localstorage, get the refresh token
	// 	//and the jwtToken
	// 	if (localStorage.getItem("user")) {
	// 		credentials = JSON.parse(localStorage.getItem("credentials"));
	// 		let avatar = await login(credentials);
	// 		if (avatar !== -1) {
	// 			token = avatar.jwtToken;
	// 			refresh = avatar.refreshToken;
	// 		}
	// 		this.setState({ loading: false })
	// 	}

	// 	//else (for now) show an alert and redirect to home
	// 	else {
	// 		// alert("not logged in");
	// 		this.setState({ loggedIn: true });
	// 		this.setState({ loading: false })
	// 	}
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
                    onHide={() => hide('avatar', 'avatarWallet')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('avatar', 'avatarWallet')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <h2 className="grid-heading">Avatar Wallet</h2>

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

export default AvatarWallet

var filterParams = {
    comparator: function (filterLocalDateAtMidnight, cellValue) {
        var dateAsString = cellValue;
        if (dateAsString == null) return -1;
        var dateParts = dateAsString.split('/');
        var cellDate = new Date(
            Number(dateParts[2]),
            Number(dateParts[1]) - 1,
            Number(dateParts[0])
        );
        if (filterLocalDateAtMidnight.getTime() === cellDate.getTime()) {
            return 0;
        }
        if (cellDate < filterLocalDateAtMidnight) {
            return -1;
        }
        if (cellDate > filterLocalDateAtMidnight) {
            return 1;
        }
    },
    browserDatePicker: true,
};

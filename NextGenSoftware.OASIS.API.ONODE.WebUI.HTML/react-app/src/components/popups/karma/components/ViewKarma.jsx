import React from 'react';
import { Modal } from 'react-bootstrap';
import Loader from "react-loader-spinner";
import { AgGridColumn, AgGridReact } from 'ag-grid-react';
class ViewKarma extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            columnDefs: [
                {
                    field: 'Date',
                    // filterParams: filterParams,
                },
                {
                    field: 'Avatar',

                },
                {
                    field: 'Positive/Negative'
                },
                {
                    field: 'Type',
                },
                {
                    field: 'Karma'
                },

                {
                    field: 'Source'
                },
                {
                    field: 'Description'
                },
                {
                    field: 'Weblink'
                },
            ],
            defaultColDef: {
                flex: 1,
                minWidth: 150,
                filter: true,
                sortable: true,
                floatingFilter: true,
                resizable: true,
                // overflow:true
            },
            rowData: null,
            // loading: true,
            // loggedIn: true,
        };
    }

    //run this after component mounts
    render() {
        const { show, hide } = this.props;
        return (
            <>
                <Modal
                    centered
                    className="custom-modal custom-popup-component"
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('karma', 'viewKarma')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('karma', 'viewKarma')}>
                            <i className="fa fa-times"></i>
                        </span>
                        <h1 className="single-heading">View Current Karma</h1>
                        
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

export default ViewKarma;
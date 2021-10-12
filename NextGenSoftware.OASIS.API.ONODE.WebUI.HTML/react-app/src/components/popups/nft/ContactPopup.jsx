import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/contactPopup.scss';

import { AgGridColumn, AgGridReact } from 'ag-grid-react';

class ContactPopup extends React.Component {

    state = {
        columnDefs: [
            { 
                field: 'Avatar'
            },
            {
                field: 'Level / Karma'
            },
            {
                field: 'Beamed In'
            },
            {
                field: 'Last Beamed In'
            },
            {
                field: 'Add to Contacts'
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
        rowData: [
            {
                name:'zunair',
                age :12
            }
        ],
    };
    
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-contact-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('nft', 'contactPopup')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('nft', 'contactPopup')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container contact-Popup default-popup">
                            <div className="popup-content">
                                <h2>Contacts</h2>
                                <div className="button-bar">
                                    <button className="single-btn" >Friends</button>
                                    <button className="single-btn">Family</button>
                                    <button className="single-btn">Colleagues</button>
                                    <button className="single-btn">My list 1</button>
                                    <button className="single-btn">My list 2</button>    
                                </div>   
                            </div>
                            
                            <div className="ag-theme-alpine custom-ag-parent">
                                <AgGridReact
                                    columnDefs={this.state.columnDefs}
                                    defaultColDef={this.state.defaultColDef}
                                    // onGridReady={this.onGridReady}
                                    rowData={this.state.rowData}
                                />
                            </div>

                            <div className="right-btn">
                                <button className="single-btn" >Find Avatar</button>
                                <button className="single-btn" >Create List</button>
                            </div>
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default ContactPopup;
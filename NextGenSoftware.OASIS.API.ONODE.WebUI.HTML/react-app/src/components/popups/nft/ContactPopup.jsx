import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/contact-popup.scss';

import { AgGridReact } from 'ag-grid-react';

var filterParams = {
    comparator: function (filterLocalDateAtMidnight, cellValue) {
        var dateAsString = cellValue;
        if (dateAsString === null) return -1;
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

class ContactPopup extends React.Component {

    state = {
        columnDefs: [
            { 
                field: 'athlete'
            },
            {
                field: 'age',
                filter: 'agNumberColumnFilter',
                maxWidth: 100,
            },
            { field: 'country' },
            {
                field: 'year',
                maxWidth: 100,
            },
            {
                field: 'date',
                filter: 'agDateColumnFilter',
                filterParams: filterParams,
            },
            { field: 'sport' },
            {
                field: 'gold',
                filter: 'agNumberColumnFilter',
            },
            {
                field: 'silver',
                filter: 'agNumberColumnFilter',
            },
            {
                field: 'bronze',
                filter: 'agNumberColumnFilter',
            },
            {
                field: 'total',
                filter: false,
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
                                <h1 className="single-heading">Contacts</h1>
                                <div className="button-bar">
                                    <button className="sm-button" >Friends</button>
                                    <button className="sm-button">Family</button>
                                    <button className="sm-button">Colleagues</button>
                                    <button className="sm-button">My list 1</button>
                                    <button className="sm-button">My list 2</button>    
                                </div>   
                            </div>
                            
                            <div className="ag-theme-alpine custom-ag-parent">
                                <AgGridReact
                                    columnDefs={this.state.columnDefs}
                                    defaultColDef={this.state.defaultColDef}
                                    onGridReady={this.onGridReady}
                                    rowData={this.state.rowData}
                                />
                            </div>

                            <div className="right-btn">
                                <button className="sm-button" >Find Avatar</button>
                                <button className="sm-button" >Create List</button>
                            </div>
                            
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default ContactPopup;
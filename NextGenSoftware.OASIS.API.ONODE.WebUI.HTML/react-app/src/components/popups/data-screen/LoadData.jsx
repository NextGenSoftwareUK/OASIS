import React from 'react';

import { Modal } from 'react-bootstrap';
import { AgGridColumn, AgGridReact } from 'ag-grid-react';

import '../../../assets/scss/data-screen.scss';

class LoadData extends React.Component {

    rowData = [
        {make: "Toyota", model: "Celica", price: 35000},
        {make: "Ford", model: "Mondeo", price: 32000},
        {make: "Porsche", model: "Boxter", price: 72000}
    ];

    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component light-custom-popup" 
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('loadData')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('loadData')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="ag-theme-alpine custom-ag-parent">
                            <AgGridReact
                                rowData={this.rowData}>
                                <AgGridColumn field="date"></AgGridColumn>
                                <AgGridColumn field="size"></AgGridColumn>
                                <AgGridColumn field="file"></AgGridColumn>
                                <AgGridColumn field="edit"></AgGridColumn>
                                <AgGridColumn field="view"></AgGridColumn>
                                <AgGridColumn field="delete"></AgGridColumn>
                            </AgGridReact>
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default LoadData;
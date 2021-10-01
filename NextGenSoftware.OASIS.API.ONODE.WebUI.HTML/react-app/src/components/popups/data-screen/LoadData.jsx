import React from 'react';

import { Modal } from 'react-bootstrap';
import { AgGridColumn, AgGridReact } from 'ag-grid-react';

import '../../../assets/scss/data-screen.scss';

class LoadData extends React.Component {

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
                    className="custom-modal custom-popup-component light-custom-popup" 
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('loadData')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('loadData')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <h2 className="grid-heading">Data</h2>

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
 
export default LoadData;

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
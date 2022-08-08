import React from 'react';
import { Modal } from 'react-bootstrap';
// import Loader from "react-loader-spinner";
import { AgGridReact } from 'ag-grid-react';
import oasisApi from "oasis-api";

class ViewKarma extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            columnDefs: [
                { field: 'date' },
                { field: 'avatar' },
                { field: 'positive/negative' },
                { field: 'type' },
                { field: 'karma' },
                { field: 'source' },
                { field: 'description' },
                { field: 'weblink' },
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
            // loading: true,
            // loggedIn: true,
        };
    }

    onGridReady = async (params) => {
        this.gridApi = params.api;
        this.gridColumnApi = params.columnApi;
        let karmaRecords = []

        const avatarApi = new oasisApi.Avatar()
        const karmaApi = new oasisApi.Karma()

        const avatarRes = await avatarApi.getAll()
        if(!avatarRes.error){
          const avatars = avatarRes.data.result
          for(let i=0; i<=avatars.length-1; i++){
            const avatar = avatars[i]
            const karmaRes = await karmaApi.getKarmaAkashicRecordsForAvatar(avatar.avatarId)
            if(!karmaRes.error){
              if(karmaRes.data.result){
                const karmas = karmaRes.data.result
                for(let j=0; j<=karmas.length-1; j++){
                  const karma = karmas[j]
                  console.log(karma)
                  const karmaRecord = {
                    date: karma.date,
                    avatar: avatar.username,
                    'positive/negative': karma.karmaTypePositive.value,
                    type: karma.karmaTypePositive.name,
                    source: karma.karmaSource.name,
                    description: karma.karmaSourceDesc,
                    weblink: 'oasisplatform.world',
                    karma: karma.karma
                  }
                  karmaRecords.push(karmaRecord)
                  console.log(karmaRecord)
                };
              }
            }
          };
        }
        console.log(karmaRecords);
        this.setState({rowData: karmaRecords})
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

import React, { Component } from 'react';

import AddData from './components/AddData';
import LoadData from './components/LoadData';
import OffChainManagement from './components/OffChainManagement';
import CrossChainManagement from './components/CrossChainManagement';

import "../../../assets/scss/data-screen.scss";

class DataScreen extends React.Component {

    render() {
        return(
            <>
                <AddData
                    show={this.props.data.sendData}
                    hide={this.props.toggleScreenPopup}
                />

                <LoadData
                    show={this.props.data.loadData}
                    hide={this.props.toggleScreenPopup}
                />

                <OffChainManagement
                    show={this.props.data.offChainManagement}
                    hide={this.props.toggleScreenPopup}
                />

                <CrossChainManagement
                    show={this.props.data.crossChainManagement}
                    hide={this.props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default DataScreen;
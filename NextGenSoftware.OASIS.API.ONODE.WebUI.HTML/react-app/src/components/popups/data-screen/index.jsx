import React from 'react';

import AddData from './components/AddData';
import LoadData from './components/LoadData';
import OffChainManagement from './components/OffChainManagement';
import CrossChainManagement from './components/CrossChainManagement';

import "../../../assets/scss/data-screen.scss";
import SearchData from './components/SearchData';
import ManageData from './components/ManageData';

class DataScreen extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <AddData
                    show={props.data.sendData}
                    hide={props.toggleScreenPopup}
                />

                <LoadData
                    show={props.data.loadData}
                    hide={props.toggleScreenPopup}
                />

                <ManageData
                    show={props.data.manageData}
                    hide={props.toggleScreenPopup}
                />

                <OffChainManagement
                    show={props.data.offChainManagement}
                    hide={props.toggleScreenPopup}
                />

                <CrossChainManagement
                    show={props.data.crossChainManagement}
                    hide={props.toggleScreenPopup}
                />

                <SearchData
                    show={props.data.searchData}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default DataScreen;
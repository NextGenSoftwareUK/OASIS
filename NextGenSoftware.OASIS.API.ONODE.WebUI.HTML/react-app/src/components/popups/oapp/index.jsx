import React from 'react';
import CreateOAPP from './components/CreateOAPP';
import DeployOAPP from './components/DeployOAPP';
import DownloadOurWorld from './components/DownloadOurWorld';
import EditOAPP from './components/EditOAPP';
import InstallOAPP from './components/InstallOAPP';
import LaunchOAPP from './components/LaunchOAPP';
import ManageOAPP from './components/ManageOAPP';

import SearchOAPP from './components/SearchOAPP';

class OAPP extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <CreateOAPP
                    show={props.oapp.createOAPP}
                    hide={props.toggleScreenPopup}
                />

                <DeployOAPP
                    show={props.oapp.deployOAPP}
                    hide={props.toggleScreenPopup}
                />

                <DownloadOurWorld
                    show={props.oapp.downloadOurWorld}
                    hide={props.toggleScreenPopup}
                />

                <EditOAPP
                    show={props.oapp.editOAPP}
                    hide={props.toggleScreenPopup}
                />

                <InstallOAPP
                    show={props.oapp.installOAPP}
                    hide={props.toggleScreenPopup}
                />

                <LaunchOAPP
                    show={props.oapp.launchOAPP}
                    hide={props.toggleScreenPopup}
                />

                <ManageOAPP
                    show={props.oapp.manageOAPP}
                    hide={props.toggleScreenPopup}
                />

                <SearchOAPP
                    show={props.oapp.searchOAPP}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default OAPP;
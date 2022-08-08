import React from 'react';

import ManageMission from './components/ManageMission';
import SearchMission from './components/SearchMission';
import ViewMission from './components/ViewMission';

class Mission extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ViewMission
                    show={props.mission.viewMission}
                    hide={props.toggleScreenPopup}
                />

                <ManageMission
                    show={props.mission.manageMission}
                    hide={props.toggleScreenPopup}
                />

                <SearchMission
                    show={props.mission.searchMission}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Mission;
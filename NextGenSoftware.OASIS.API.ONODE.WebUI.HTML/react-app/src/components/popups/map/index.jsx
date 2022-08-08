import React from 'react';
import Add2dObjectMap from './components/Add2dObjectMap';
import Add3dObjectMap from './components/Add3dObjectMap';
import AddQuestToMap from './components/AddQuestToMap';
import DownloadOurWorld from './components/DownloadOurWorld';
import ManageMap from './components/ManageMap';
import SearchMap from './components/SearchMap';
import PlotRouteOnMap from './components/PlotRouteOnMap';
import ViewGlobal3dMap from './components/ViewGlobal3dMap';
import ViewHalonsOnMap from './components/ViewHalonsOnMap';
import ViewOappOnMap from './components/ViewOappOnMap';
import ViewQuestOnMap from './components/ViewQuestOnMap';

class Map extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <Add2dObjectMap
                    show={props.map.add2dObjectMap}
                    hide={props.toggleScreenPopup}
                />

                <Add3dObjectMap
                    show={props.map.add3dObjectMap}
                    hide={props.toggleScreenPopup}
                />

                <AddQuestToMap
                    show={props.map.addQuestToMap}
                    hide={props.toggleScreenPopup}
                />

                <DownloadOurWorld
                    show={props.map.downloadOurWorld}
                    hide={props.toggleScreenPopup}
                />

                <ManageMap
                    show={props.map.manageMap}
                    hide={props.toggleScreenPopup}
                />

                <PlotRouteOnMap
                    show={props.map.plotRouteOnMap}
                    hide={props.toggleScreenPopup}
                />

                <SearchMap
                    show={props.map.searchMap}
                    hide={props.toggleScreenPopup}
                />

                <ViewGlobal3dMap
                    show={props.map.viewGlobal3dMap}
                    hide={props.toggleScreenPopup}
                />

                <ViewHalonsOnMap
                    show={props.map.viewHalonsOnMap}
                    hide={props.toggleScreenPopup}
                />

                <ViewOappOnMap
                    show={props.map.viewOappOnMap}
                    hide={props.toggleScreenPopup}
                />

                <ViewQuestOnMap
                    show={props.map.viewQuestOnMap}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Map;
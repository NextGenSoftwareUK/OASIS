import React from 'react';
import ManageEggs from './components/ManageEggs';
import SearchEggs from './components/SearchEggs';
import ViewEggs from './components/ViewEggs';

class Eggs extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ViewEggs
                    show={props.eggs.viewEggs}
                    hide={props.toggleScreenPopup}
                />

                <ManageEggs
                    show={props.eggs.manageEggs}
                    hide={props.toggleScreenPopup}
                />

                <SearchEggs
                    show={props.eggs.searchEggs}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Eggs;
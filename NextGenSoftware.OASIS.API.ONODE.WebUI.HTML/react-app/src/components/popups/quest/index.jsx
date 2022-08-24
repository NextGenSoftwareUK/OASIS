import React from 'react';

import ManageQuest from './components/ManageQuest';
import SearchQuest from './components/SearchQuest';
import ViewQuest from './components/ViewQuest';

class Quest extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ViewQuest
                    show={props.quest.viewQuest}
                    hide={props.toggleScreenPopup}
                />

                <ManageQuest
                    show={props.quest.manageQuest}
                    hide={props.toggleScreenPopup}
                />

                <SearchQuest
                    show={props.quest.searchQuest}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Quest;
import React  from 'react';

import "../../../assets/scss/data-screen.scss";
import ViewKarma from './components/ViewKarma';
import ViewAvatarKarma from './components/ViewAvatarKarma';
import SearchKarma from './components/SearchKarma';
import VoteKarma from './components/VoteKarma';
class Karma extends React.Component {
    render() { 
        const props=this.props;
        return(
            <>
                <ViewKarma
                    show={props.karma.viewKarma}
                    hide={props.toggleScreenPopup}
                />
                <SearchKarma 
                    show={props.karma.searchKarma}
                    hide={props.toggleScreenPopup}
                />
                <VoteKarma 
                    show={props.karma.voteKarma}
                    hide={props.toggleScreenPopup}
                />
                <ViewAvatarKarma 
                    show={props.karma.viewAvatarKarma}
                    hide={props.toggleScreenPopup}
                />
            </>
        );
    }
}
 
export default Karma;
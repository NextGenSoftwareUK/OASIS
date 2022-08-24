import React from 'react';

import SearchProfiles from './components/SearchProfiles';
import ViewAchievements from './components/ViewAchievements';
import ViewLeagues from './components/ViewLeagues';
import ViewTournaments from './components/ViewTournaments';

class Game extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <SearchProfiles
                    show={props.game.searchProfiles}
                    hide={props.toggleScreenPopup}
                />

                <ViewTournaments
                    show={props.game.viewTournaments}
                    hide={props.toggleScreenPopup}
                />

                <ViewLeagues
                    show={props.game.viewLeagues}
                    hide={props.toggleScreenPopup}
                />

                <ViewAchievements
                    show={props.game.viewAchievements}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Game;
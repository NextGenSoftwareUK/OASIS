import React from 'react';

import AcceptInvite from './components/AcceptInvite';
import DonateSeeds from './components/DonateSeeds';
import PayWithSeeds from './components/PayWithSeeds';
import RewardSeeds from './components/RewardSeeds';
import SendInvite from './components/SendInvite';

import '../../../assets/scss/seeds-popup.scss';
import ViewSeeds from './components/ViewSeeds';
import ManageSeeds from './components/ManageSeeds';
import SearchSeeds from './components/SearchSeeds';
import ViewOrganizations from './components/ViewOrganizations';

class Seeds extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <AcceptInvite
                    show={props.seeds.acceptInvite}
                    hide={props.toggleScreenPopup}
                />

                <DonateSeeds
                    show={props.seeds.donateSeeds}
                    hide={props.toggleScreenPopup}
                />

                <PayWithSeeds
                    show={props.seeds.payWithSeeds}
                    hide={props.toggleScreenPopup}
                />

                <RewardSeeds
                    show={props.seeds.rewardSeeds}
                    hide={props.toggleScreenPopup}
                />

                <SendInvite
                    show={props.seeds.sendInvite}
                    hide={props.toggleScreenPopup}
                />

                <SearchSeeds
                    show={props.seeds.searchSeeds}
                    hide={props.toggleScreenPopup}
                />

                <ManageSeeds
                    show={props.seeds.manageSeeds}
                    hide={props.toggleScreenPopup}
                />

                <ViewOrganizations
                    show={props.seeds.viewOrganizations}
                    hide={props.toggleScreenPopup}
                />

                <ViewSeeds
                    show={props.seeds.viewSeeds}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Seeds;
import React, { Component } from 'react';

import AcceptInvite from './components/AcceptInvite';
import DonateSeeds from './components/DonateSeeds';
import PayWithSeeds from './components/PayWithSeeds';
import RewardSeeds from './components/RewardSeeds';
import SendInvite from './components/SendInvite';

import '../../../assets/scss/seeds-popup.scss';

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
            </>
        )
    }
}

export default Seeds;
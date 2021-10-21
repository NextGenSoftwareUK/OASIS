import React, { Component } from 'react';

import DonateSeeds from './components/DonateSeeds';
import PayWithSeeds from './components/PayWithSeeds';
import RewardSeeds from './components/RewardSeeds';
import SendInvite from './components/SendInvite';

import '../../../assets/scss/seeds-popup.scss';

class Seeds extends React.Component {

    render() {
        return(
            <>
                {/* <AcceptInvite
                    show={this.props.seeds.acceptInvite}
                    hide={this.props.toggleScreenPopup}
                /> */}

                <DonateSeeds
                    show={this.props.seeds.donateSeeds}
                    hide={this.props.toggleScreenPopup}
                />

                <PayWithSeeds
                    show={this.props.seeds.payWithSeeds}
                    hide={this.props.toggleScreenPopup}
                />

                <RewardSeeds
                    show={this.props.seeds.rewardSeeds}
                    hide={this.props.toggleScreenPopup}
                />

                <SendInvite
                    show={this.props.seeds.sendInvite}
                    hide={this.props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Seeds;
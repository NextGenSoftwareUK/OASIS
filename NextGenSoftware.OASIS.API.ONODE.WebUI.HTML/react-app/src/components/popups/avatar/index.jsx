import React, { Component } from 'react';

import AvatarDetail from './components/AvatarDetail';
import AvatarWallet from './components/AvatarWallet';
import ViewAvatar from './components/ViewAvatar';

class Avatar extends React.Component {

    render() {
        return(
            <>
                <AvatarDetail
                    show={this.props.avatar.avatarDetail}
                    hide={this.props.toggleScreenPopup}
                />

                <AvatarWallet
                    show={this.props.avatar.avatarWallet}
                    hide={this.props.toggleScreenPopup}
                />

                <ViewAvatar
                    show={this.props.avatar.viewAvatar}
                    hide={this.props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Avatar;
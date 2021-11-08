import React, { Component } from 'react';

// import AvatarDetail from './components/AvatarDetail';
import AvatarWallet from './components/AvatarWallet';
import ViewAvatar from './components/ViewAvatar';

class Avatar extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                {/* <AvatarDetail
                    show={props.avatar.avatarDetail}
                    hide={props.toggleScreenPopup}
                /> */}

                <AvatarWallet
                    show={props.avatar.avatarWallet}
                    hide={props.toggleScreenPopup}
                />

                <ViewAvatar
                    show={props.avatar.viewAvatar}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Avatar;
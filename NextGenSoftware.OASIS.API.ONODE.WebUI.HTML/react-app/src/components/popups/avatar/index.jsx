import React from 'react';

// import AvatarDetail from './components/AvatarDetail';
import AvatarWallet from './components/AvatarWallet';
import EditAvatar from './components/EditAvatar';
import SearchAvatar from './components/SearchAvatar';
import ViewAvatar from './components/ViewAvatar';

class Avatar extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ViewAvatar
                    show={props.avatar.viewAvatar}
                    hide={props.toggleScreenPopup}
                />

                <EditAvatar
                    show={props.avatar.editAvatar}
                    hide={props.toggleScreenPopup}
                />

                <SearchAvatar
                    show={props.avatar.searchAvatar}
                    hide={props.toggleScreenPopup}
                />

                <AvatarWallet
                    show={props.avatar.avatarWallet}
                    hide={props.toggleScreenPopup}
                />
            </>
        )
    }
}

export default Avatar;
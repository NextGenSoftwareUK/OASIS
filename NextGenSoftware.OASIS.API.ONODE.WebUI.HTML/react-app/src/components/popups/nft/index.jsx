import React from 'react';

import ManageOasisNft from './components/ManageOasisNft';
import PurchaseOasisNft from './components/PurchaseOasisNft';
import ViewOasisNft from './components/ViewOasisNft';
import PurchaseOasisVirtualLandNft from './components/PurchaseOasisVirtualLandNft';
import SearchOasisNft from './components/SearchOasisNft';

class Quest extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ViewOasisNft
                    show={props.nft.viewOasisNft}
                    hide={props.toggleScreenPopup}
                />

                <ManageOasisNft
                    show={props.nft.manageOasisNft}
                    hide={props.toggleScreenPopup}
                />

                <PurchaseOasisNft
                    show={props.nft.purchaseOasisNft}
                    hide={props.toggleScreenPopup}
                />

                <PurchaseOasisVirtualLandNft
                    show={props.nft.purchaseOasisVirtualLandNft}
                    hide={props.toggleScreenPopup}
                />

                <SearchOasisNft
                    show={props.nft.searchOasisNft}
                    hide={props.toggleScreenPopup}
                />
                
            </>
        )
    }
}

export default Quest;
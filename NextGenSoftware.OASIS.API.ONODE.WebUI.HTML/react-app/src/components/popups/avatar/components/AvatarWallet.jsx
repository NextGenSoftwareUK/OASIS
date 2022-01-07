import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../../assets/scss/avatar-popup.scss';

class AvatarWallet extends React.Component {
    render() {
        const { show, hide } = this.props;

        return (
            <>
                <Modal
                    centered
                    className="custom-modal custom-popup-component w-100 "
                    show={show}
                    onHide={() => hide('avatar', 'avatarWallet')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('avatar', 'avatarWallet')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="page new-skin">
                            {/* Container */}
                            <div className="container opened" data-animation-in="fadeInLeft" data-animation-out="fadeOutLeft">
                                
                                    <h1 class="single-heading">Avatar Wallet</h1>    
                                {/* Card - Started */}
                                <div className="card-started" id="home-card">
                                    {/* Profile */}
                                    <div className="profile no-photo">
                                        <div className="slide" />
                                        <div className="title">Username</div>
                                        <div className="subtitle"> User Level </div>
                                        <div className="lnks">
                                            <a href="#" className="lnk">
                                                <span className="text">2D Avatar</span>
                                            </a>
                                            <a href="#" className="lnk discover">
                                                <span className="text">3d Avatar</span>
                                            </a>
                                        </div>
                                    </div>
                                </div>
                                {/* Card - 1 */}
                                {/* <div className="card-inner animated" >
                                    <div className="card-wrap"> */}
                                        {/* Content */}
                                        {/* <div className="content">
                                            <div className="title">Card 1</div>
                                        </div>
                                    </div>
                                </div> */}
                            </div>
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}

export default AvatarWallet;
import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/seeds-popup.scss';

class SendInvite extends React.Component {
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('seeds', 'sendInvite')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('seeds', 'sendInvite')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="seed-container paywith-seeds">
                                <h1 className="fs-34"> 
                                    Send Invite to Join with Seeds
                                </h1>
                                <div className="form-container">
                                    <form>
                                        <p className="single-form-row">
                                            <label>FROM: Avatar or Seed Username</label>
                                             <input type="text" placeholder="username" />
                                        </p>

                                        <p className="single-form-row">
                                            <label>TO: Avatar or Seed Username</label>
                                             <input type="text"  placeholder="username"/>
                                        </p>

                                        <p className="single-form-row">
                                            <label>Message</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row mb-30">
                                            <label>Seeds to Gift</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row mb-30">
                                            <label>Show Quality</label>
                                            <input type="text" />
                                        </p>
                                        
                                        <p className="single-form-row btn-right">
                                            <button 
                                                className="send-button-container" 
                                                type="submit"
                                            >Send</button>
                                        </p>
                                    </form>
                                </div>
                            </div>
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default SendInvite ;
import React from 'react';

import { Modal } from 'react-bootstrap';

class AcceptInvite extends React.Component {
    render() {
        const { show, hide } = this.props;

        return (
            <>
                <Modal
                    centered
                    className="custom-modal custom-popup-component"
                    show={show}
                    onHide={() => hide('seeds', 'acceptInvite')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('seeds', 'acceptInvite')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="seed-container paywith-seeds">
                                <h1 className="single-heading">
                                    Accept Invite to Join Seeds
                                </h1>
                                <div className="form-container">
                                    <form>
                                        <p className="single-form-row">
                                            <label className="radio-btn">
                                                <input type="radio" id="html" name="fav_language" value="HTML" />
                                                Avatar
                                                <input type="radio" id="html" name="fav_language" value="HTML" />
                                                Seed Username
                                            </label>
                                            <input type="text" placeholder="username" />
                                        </p>

                                        <p className="single-form-row  mb-30">
                                            <label>Invite Secret</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row btn-right">
                                            <button
                                                className=" btn-width sm-button"
                                                type="submit"
                                            >Accept Invitation</button>
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

export default AcceptInvite;
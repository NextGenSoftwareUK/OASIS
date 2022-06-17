import React from "react";
import { Modal } from "react-bootstrap";
import '../../../assets/scss/coming-soon.scss';
import InfoIcon from '../../../assets/images/icon-info.svg'
class ComingSoon extends React.Component {
    render() {
        return (
            <Modal
                size="sm"
                show={this.props.show}
                dialogClassName="modal-90w"
                onHide={this.props.toggleScreenPopup}
            >
                <Modal.Body className="text-center coming-soon">
                    <img
                        src={InfoIcon}
                        alt="icon"
                    />
                    <h2>Coming Soon</h2>
                    <p>This module is coming soon...</p>
                    <button onClick={this.props.toggleScreenPopup}>OK</button>
                </Modal.Body>
            </Modal>
        );
    }
}

export default ComingSoon;

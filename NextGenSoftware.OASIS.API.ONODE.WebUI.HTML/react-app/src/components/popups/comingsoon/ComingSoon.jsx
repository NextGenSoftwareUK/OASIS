import React from "react";
import { Modal } from "react-bootstrap";

class ComingSoon extends React.Component {
    render() {
        return (
            <Modal
                size="xl"
                show={this.props.show}
                dialogClassName="modal-90w"
                onHide={this.props.toggleScreenPopup}
            >
                <Modal.Header closeButton>
                    <Modal.Title>Coming Soon</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <h1>Coming Soon</h1>
                </Modal.Body>
            </Modal>
        );
    }
}

export default ComingSoon;

import React from 'react';
import { Modal } from "react-bootstrap";
import '../../../../assets/scss/coming-soon.scss';
import InfoIcon from '../../../../assets/images/icon-info.svg'

class Add2dObjectMap extends React.Component {
    state = {  } 
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal
                    size="sm"
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('map', 'add2dObjectMap')}
                >
                    <Modal.Body className="text-center coming-soon">
                        <img
                            src={InfoIcon}
                            alt="icon"
                        />
                        <h2>Coming Soon</h2>
                        <p>This module is coming soon...</p>
                        <button onClick={() => hide('map', 'add2dObjectMap')}>OK</button>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default Add2dObjectMap;
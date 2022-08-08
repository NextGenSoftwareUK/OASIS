import React from 'react';
import { Modal } from "react-bootstrap";
import '../../../../assets/scss/coming-soon.scss';
import InfoIcon from '../../../../assets/images/icon-info.svg'

class MongoDb extends React.Component {
    state = {  } 
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal
                    size="sm"
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('provider', 'mongoDb')}
                >
                    <Modal.Body className="text-center coming-soon">
                        <img
                            src={InfoIcon}
                            alt="icon"
                        />
                        <h2>UI Coming Soon</h2>
                        <p>You can use this functionality directly by accessing the OASIS API from the Developer menu.</p>
                        <button onClick={() => hide('provider', 'mongoDb')}>OK</button>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default MongoDb;
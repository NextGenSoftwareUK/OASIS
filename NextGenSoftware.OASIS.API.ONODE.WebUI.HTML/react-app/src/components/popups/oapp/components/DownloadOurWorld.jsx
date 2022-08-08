import React from 'react';
import { Modal } from "react-bootstrap";
import '../../../../assets/scss/coming-soon.scss';
import InfoIcon from '../../../../assets/images/icon-info.svg'

class DownloadOurWorld extends React.Component {
    state = {  } 
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal
                    size="sm"
                    show={show}
                    dialogClassName="modal-90w"
                    onHide={() => hide('oapp', 'downloadOurWorld')}
                >
                    <Modal.Body className="text-center coming-soon">
                        <img
                            src={InfoIcon}
                            alt="icon"
                        />
                        <h2>STAR ODK Coming Soon.</h2>
                        <p>This is functionaliy that is built in the STAR ODK and will soon be released. You can then download STAR ODK from the Developer menu.</p>
                        <button onClick={() => hide('oapp', 'downloadOurWorld')}>OK</button>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
 
export default DownloadOurWorld;
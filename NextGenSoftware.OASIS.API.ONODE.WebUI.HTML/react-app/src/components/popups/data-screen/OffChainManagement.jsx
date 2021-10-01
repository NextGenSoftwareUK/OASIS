import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/data-screen.scss';

class OffChainManagement extends React.Component {
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('offChainManagement')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('offChainManagement')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="data-screen-container">
                                <h2>Off Chain Management</h2>

                                <div className="form-container">
                                    <form>

                                        <h3> On Chain Provider</h3>
                                        
                                        <div className="grid-container">

                                            <div className="single-form-col">
                                                <label> </label>
                                                <textarea></textarea>
                                            </div>
                                                
                                            <div className="single-form-col">
                                                <button>ADD</button>
                                                <button>Add ALL</button>
                                                <button>REMOVE</button>
                                                <button>REMOVE ALL</button>
                                            </div>

                                            <div className="single-form-col">
                                                <label> </label>
                                                <textarea></textarea>
                                            </div>

                                        </div>

                                        <div className="single-form-row">
                                            <button 
                                                className="submit-button" 
                                                type="submit"
                                            >Save</button>
                                        </div>
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
 
export default OffChainManagement;
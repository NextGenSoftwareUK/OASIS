import React from 'react';

import { Modal } from 'react-bootstrap';

class AddData extends React.Component {
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('data','sendData')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('data', 'sendData')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="data-screen-container">
                                <h1 className="single-heading">Data</h1>

                                <div className="form-container">
                                    <form>
                                        <p className="single-form-row">
                                            <label>Provider: </label>
                                            <span className="have-selectbox">
                                                <select className="custom-selectbox">
                                                    <option>EOSIS</option>
                                                    <option>EOSIS - 1</option>
                                                    <option>EOSIS - 2</option>
                                                </select>
                                                <i className="fa fa-angle-down"></i>
                                            </span>

                                            <span className="have-icon"></span>
                                        </p>

                                        <p className="single-form-row">
                                            <label>Json: </label>
                                            <textarea></textarea>
                                        </p>

                                        <p className="single-form-row">
                                            <label>File: </label>
                                            <input type="file" />
                                        </p>

                                        <p className="single-form-row">
                                            <button 
                                                className="submit-button sm-button" 
                                                type="submit"
                                            >Save</button>
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
 
export default AddData;
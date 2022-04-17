import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/nft.scss';
import ProviderDropdown from '../../common/ProviderDropdown';

class Solana extends React.Component {
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('nft', 'solana')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('nft', 'solana')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="nft-container solana">
                                <h1 className="single-heading">Solana Provider</h1>

                                <div className="form-container">
                                    <form>
                                        <p className="single-form-row">
                                            <label>Solana : </label>
                                             <input type="text" />
                                        </p>

                                        <ProviderDropdown />

                                        <p className="single-form-row">
                                            <label>To Avatar :</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row">
                                            <label>Or Wallet Address :</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row">
                                            <label>Message :</label>
                                            <textarea ></textarea>
                                        </p>
                                        
                                        <p className="single-form-row btn-right">
                                            <button 
                                                className="sm-button" 
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
 
export default Solana;
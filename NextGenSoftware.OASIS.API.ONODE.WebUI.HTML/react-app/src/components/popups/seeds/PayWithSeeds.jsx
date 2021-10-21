import React from 'react';

import { Modal } from 'react-bootstrap';

import '../../../assets/scss/seeds-popup.scss';

class PayWithSeeds extends React.Component {
    render() { 
        const { show, hide } = this.props;

        return (
            <>
                <Modal 
                    centered 
                    className="custom-modal custom-popup-component" 
                    show={show} 
                    onHide={() => hide('seeds', 'payWithSeeds')}
                >
                    <Modal.Body>
                        <span className="form-cross-icon" onClick={() => hide('seeds', 'payWithSeeds')}>
                            <i className="fa fa-times"></i>
                        </span>

                        <div className="popup-container default-popup">
                            <div className="seed-container paywith-seeds">
                                <h1>
                                    <select
                                        className="custom-selectbox"
                                    >
                                        <option value="Pay">Pay</option>
                                        <option value="Donate">Donate</option>
                                        <option value="Reward">Reward</option>
                                    </select>
                                    with Seeds
                                </h1>
                                <div className="form-container">
                                    <form>
                                        <p className="single-form-row">
                                            <label>FROM: Avatar or Seed Username</label>
                                             <input type="text"  placeholder="username"/>
                                        </p>

                                        <p className="single-form-row">
                                            <label>TO: Avatar or Seed Username</label>
                                             <input type="text"  placeholder="username"/>
                                        </p>

                                        <p className="single-form-row">
                                            <label>Amount</label>
                                            <input type="text" />
                                        </p>

                                        <p className="single-form-row mb-30">
                                            <label>Note</label>
                                            <input type="text" />
                                        </p>
                                        
                                        <p className="single-form-row btn-right">
                                            <button 
                                                className="send-button-container" 
                                                type="submit"
                                            >Pay</button>
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
 
export default PayWithSeeds ;
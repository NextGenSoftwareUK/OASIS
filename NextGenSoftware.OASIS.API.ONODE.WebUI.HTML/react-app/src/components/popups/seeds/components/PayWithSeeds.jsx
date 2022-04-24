import React from 'react';

import { Modal } from 'react-bootstrap';

class PayWithSeeds extends React.Component {
    constructor(){
      super()
      this.state = {
        avatar: '',
        seedUser: '',
        amount: '',
        note: ''
      }
    }

    handleChange = (e) => {
      this.setState({[e.target.name]: e.target.value})
      console.log(this.state);
    }

    handleSubmit=(e) => {
      e.preventDefault()
    }
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
                                <h1 className="single-heading">
                                    Pay with Seeds
                                </h1>
                                <div className="form-container">
                                    <form onSubmit={this.handleSubmit}>
                                        <p className="single-form-row">
                                            <label className="single-radio-btn">
                                                <input type="radio" id="html" name="fav_language" value="HTML" />
                                                Avatar
                                            </label>
                                            <input type="text" placeholder="username" name="avatar" value={this.state.avatar} onChange={this.handleChange} />
                                        </p>

                                        <p className="single-form-row">
                                            <label className="single-radio-btn">
                                                <input type="radio" id="html" name="fav_language" value="HTML" />
                                                Seed Username
                                            </label>
                                            <input type="text" name="seedUser" value={this.state.seedUser} onChange={this.handleChange} placeholder="username" />
                                        </p>

                                        <p className="single-form-row">
                                            <label>Amount</label>
                                            <input type="text" name="amount" value={this.state.amount} onChange={this.handleChange} />
                                        </p>

                                        <p className="single-form-row mb-30">
                                            <label>Note</label>
                                            <input type="text" name="note" value={this.state.note} onChange={this.handleChange} />
                                        </p>

                                        <p className="single-form-row btn-right">
                                            <button
                                                className="sm-button"
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

export default PayWithSeeds;

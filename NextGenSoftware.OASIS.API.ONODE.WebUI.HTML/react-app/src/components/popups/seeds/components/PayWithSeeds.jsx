import React from 'react';

import { Modal } from 'react-bootstrap';
import axios from "axios";
import { toast } from "react-toastify";

class PayWithSeeds extends React.Component {

    constructor(){
      super()
      this.state = {
        group: '',
        avatar: '',
        seedUser: '',
        amount: '',
        note: ''
      }
    }

    componentDidMount = () => {
        this.loadAllAvatarData();
    }

    loadAllAvatarData = () => {
        axios.get('https://api.oasisplatform.world/api/avatar/get-all-avatars')
        .then(response => {
            console.log(response)
            if(response.data.isError) {
                toast.error(response.data.message)
            } else {
                toast.success(response.data.result.message)
            }
            // this.props.history.goBack()
            // console.log(this.props) 
        })
        .catch(error => {
            console.log(error)
        })
    }

    handleChange = (e) => {
        console.log(e.target.value)

        if(e.target.value === "avatar_section" || e.target.value === "username_section") {
            this.setState({
                group: e.target.value
            })
        } else {
            this.setState({[e.target.name]: e.target.value})
        }
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
                                                <input 
                                                    type="radio" 
                                                    value="avatar_section" 
                                                    checked={this.state.group === "avatar_section"} 
                                                    onChange={this.handleChange} 
                                                />
                                                Avatar
                                            </label>

                                            <input 
                                                type="text" 
                                                placeholder="Avatar" 
                                                name="avatar" 
                                                value={this.state.avatar} 
                                                onChange={this.handleChange}
                                                disabled={this.state.group === "username_section"} 
                                            />
                                        </p>

                                        <h3>OR</h3>

                                        <p className="single-form-row">
                                            <label className="single-radio-btn">
                                                <input 
                                                    type="radio" 
                                                    value="username_section"
                                                    checked={this.state.group === "username_section"} 
                                                    onChange={this.handleChange}  
                                                />
                                                Seed Username
                                            </label>

                                            <input 
                                                type="text" 
                                                name="seedUser" 
                                                value={this.state.username} 
                                                onChange={this.handleChange} 
                                                placeholder="Seed Username"
                                                disabled={this.state.group === "avatar_section"} 
                                            />
                                        </p>

                                        <p className="single-form-row">
                                            <label>Amount</label>
                                            <input 
                                                type="text" 
                                                name="amount" 
                                                value={this.state.amount} 
                                                onChange={this.handleChange}
                                                placeholder="Amount"
                                                disabled={this.state.group === "avatar_section"}  
                                            />
                                        </p>

                                        <p className="single-form-row mb-30">
                                            <label>Note</label>
                                            <input 
                                                type="text" 
                                                name="note" 
                                                value={this.state.note} 
                                                onChange={this.handleChange}
                                                placeholder="Note"
                                                disabled={this.state.group === "avatar_section"}  
                                            />
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

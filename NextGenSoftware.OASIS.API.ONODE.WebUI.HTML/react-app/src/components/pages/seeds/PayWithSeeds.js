import React from 'react'
import { Component } from 'react'
import { Modal } from 'react-bootstrap'
import { Link } from 'react-router-dom'

export default class PayWithSeeds extends Component {
    constructor(props) {
        super(props)

        this.handleSeedType = this.handleSeedType.bind(this)
        this.state = {
            seedType: this.props.seedType || "Pay"
        }
    }

    handleSeedType(e) {
        this.setState({
            seedType: e.target.value
        })
    }

    componentDidUpdate(prevProps) {
        if (this.props.seedType !== prevProps.seedType) // Check if it's a new user, you can also use some unique property, like the ID  (this.props.user.id !== prevProps.user.id)
        {
            this.setState({
                seedType: this.props.seedType
            })
        }
    }
    render() {
        return (
            <Modal centered className="popup-container custom-modal" show={true}>
                <div className="popup">
                    <Link to="/" className="popup-cancel">
                        <span className="form-cross-icon">
                            <i className="fa fa-times"></i>
                        </span>
                    </Link>
                    <h1>
                        <select
                            className="payWithSeeds-select"
                            value={this.state.seedType}
                            onChange={this.handleSeedType}
                        >
                            <option value="Pay">Pay</option>
                            <option value="Donate">Donate</option>
                            <option value="Reward">Reward</option>
                        </select>
                        with Seeds
                    </h1>

                    <div className="payWithSeeds-field-group">
                        <label className="payWithSeeds-label">FROM: Avatar or Seed Username</label>
                        <input type="text" placeholder="username" />
                    </div>
                    <div className="payWithSeeds-field-group">
                        <label className="payWithSeeds-label">TO: Avatar or Seed Username</label>
                        <input type="text" placeholder="username" />
                    </div>

                    <div className="payWithSeeds-field-group">
                        <label className="payWithSeeds-label">Amount</label>
                        <input type="number" />
                    </div>
                    <div className="payWithSeeds-field-group">
                        <label className="payWithSeeds-label">Note</label>
                        <input type="text" />
                    </div>
                    <button className="popup-submit-button button">{this.state.seedType}</button>
                </div>
            </Modal>
        );
    }
}
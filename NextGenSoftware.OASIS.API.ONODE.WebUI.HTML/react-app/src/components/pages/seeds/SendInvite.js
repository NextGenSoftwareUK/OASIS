import React, { Component } from 'react'
import { Link } from 'react-router-dom'

export default class SendInvite extends Component {
  render() {
    return (
      <div className="popup-container">
        <div className="popup">
          <Link to="/" className="popup-cancel">
            <span className="form-cross-icon">
              <i className="fa fa-times"></i>
            </span>
          </Link>
          <h1>Send Invite To Join Seeds</h1>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">FROM: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
          </div>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">TO: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
          </div>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">Message</label>
            <input type="text" />
          </div>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">SEEDS To Gift</label>
            <input type="text" />
          </div>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">Sow Quality</label>
            <input type="text" />
          </div>
          <button className="popup-submit-button button">Send</button>
        </div>
      </div>
    )
  }
}

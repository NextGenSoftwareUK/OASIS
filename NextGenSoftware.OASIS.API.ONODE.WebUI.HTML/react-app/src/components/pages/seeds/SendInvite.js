import React, { Component } from 'react'
import { Link } from 'react-router-dom'

export default class SendInvite extends Component {
  render() {
    return (
      <div className="popup-container">
        <div className="payWithSeeds-popup">
          <h1>Send Invite To Join Seeds</h1>
          <div className="payWithSeeds-fields">
            <label className="payWithSeeds-label">FROM: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
            <label className="payWithSeeds-label">TO: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
          </div>
          <div className="sendInvite-row">
            <label className="payWithSeeds-label">Message</label>
            <input type="text" />
          </div>
          <div className="payWithSeeds-button-align">
            <Link to="/" className="button-inverse">Cancel</Link>
            <button className="button">Send</button>
          </div>
        </div>
      </div>
    )
  }
}

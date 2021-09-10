import React, { Component } from 'react'

export default class SendInvite extends Component {
  render() {
    return (
      <div className="payWithSeeds">
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
            <button className="button-inverse">Cancel</button>
            <button className="button">Send</button>
          </div>
        </div>
      </div>
    )
  }
}

import React, { Component } from 'react'
import { Modal } from 'react-bootstrap'
import { Link } from 'react-router-dom'

export default class AcceptInvite extends Component {
  render() {
    return (
      <Modal centered className="popup-container custom-modal" show={true}>
        <div className="popup">
          <Link to="/" className="popup-cancel">
            <span className="form-cross-icon">
              <i className="fa fa-times"></i>
            </span>
          </Link>
          <h1>Accept Invite To Join Seeds</h1>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">Recieving Account: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
          </div>
          <div className="payWithSeeds-field-group">
            <label className="payWithSeeds-label">Invite Secret</label>
            <input type="text" />
          </div>
          <button className="accept-invite-button button">Accept Invitation</button>
        </div>
      </Modal>
    )
  }
}

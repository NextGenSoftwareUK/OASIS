import React, { Component } from 'react'
import { Link } from 'react-router-dom'

export default class ViewSeeds extends Component {
  render() {
    return (
      <div className="popup-container">
        <div className="view-popup popup">
          <h1>View Seeds</h1>
          <h2>Your Seeds: 22222</h2>
          <h2>Karma: 777</h2>
          <div className="view-seeds-buttons">
            <Link className="button" to="/pay-with-seeds">Pay With Seeds</Link>
            <Link className="button" to="/donate-with-seeds">Donate With Seeds</Link>
            <Link className="button" to="/reward-with-seeds">Reward With Seeds</Link>
          </div>
        </div>
      </div>
    )
  }
}

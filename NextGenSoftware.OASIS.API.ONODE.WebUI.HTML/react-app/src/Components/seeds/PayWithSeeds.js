import React from 'react'
import { Component } from 'react'

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

  render() {
    return (
      <div className="payWithSeeds">
        <div className="payWithSeeds-popup">
          <h1>
            <select
              className="payWithSeeds-select"
              value={this.state.seedType}
              onChange={this.handleSeedType}
            >
              <option value="Pay">Pay</option>
              <option value="Donate">Donate</option>
              <option value="Reward">Reward</option>
            </select>with Seeds</h1>
          <div className="payWithSeeds-fields">
            <label className="payWithSeeds-label">FROM: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
            <label className="payWithSeeds-label">TO: Avatar or Seed Username</label>
            <input type="text" placeholder="username" />
          </div>
          <div className="payWithSeeds-row">
            <label className="payWithSeeds-label">Amount</label>
            <input type="number" />
            <label className="payWithSeeds-label">Note</label>
            <input type="text" />
          </div>
          <div className="payWithSeeds-button-align">
            <button className="button-inverse">Cancel</button>
            <button className="button">{this.state.seedType}</button>
          </div>
        </div>
      </div>
    );
  }
}

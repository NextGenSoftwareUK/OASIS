import React from 'react'
import '../CSS/PayWithSeeds.css'

export default function PayWithSeeds() {
  return (
    <div className="payWithSeeds">
      <div className="payWithSeeds-popup">
        <h1>Pay with payWithSeeds</h1>
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
          <button className="button">Pay</button>
        </div>
      </div>
    </div>
  )
}

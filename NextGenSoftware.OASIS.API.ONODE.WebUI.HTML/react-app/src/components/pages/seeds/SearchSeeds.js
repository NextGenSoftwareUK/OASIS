import React, { Component } from 'react'

export default class SearchSeeds extends Component {
  render() {
    return (
      <div className="popup-container">
        <div className="popup search-popup">
          <h1>Search Seeds</h1>
          <div className="search-seeds-row1">
            <label className="payWithSeeds-label">Avatar</label>
            <input type="text" placeholder="username" />
            <label className="payWithSeeds-label">Seeds Username</label>
            <input type="text" />
            <label htmlFor="fromDate">From:</label>
            <input type="date" id="fromDate" name="fromDate" />
            <label htmlFor="toDate">To:</label>
            <input type="date" id="toDate" name="toDate" />
          </div>
          <div className="search-seeds-row2">
            <div className="search-checkbox">
              <label htmlFor="searchType">Type:</label>
              <div className="search-checkbox-group">
                <label htmlFor="search-donate">Donate</label>
                <input type="checkbox" name="search-donate" id="search-donate" />
              </div>
              <div className="search-checkbox-group">
                <label htmlFor="search-reward">Reward</label>
                <input type="checkbox" name="search-reward" id="search-reward" />
              </div>
              <div className="search-checkbox-group">
                <label htmlFor="search-pay">Pay</label>
                <input type="checkbox" name="search-pay" id="search-pay" />
              </div>
              <div className="search-checkbox-group">
                <label htmlFor="search-sent">Sent</label>
                <input type="checkbox" name="search-sent" id="search-sent" />
              </div>
              <div className="search-checkbox-group">
                <label htmlFor="search-Received">Received</label>
                <input type="checkbox" name="search-received" id="search-received" />
              </div>
            </div>
            <button className="button">Search</button>
          </div>
        </div>
      </div>
    )
  }
}

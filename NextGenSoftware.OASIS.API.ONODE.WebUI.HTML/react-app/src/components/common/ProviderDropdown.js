import React from 'react';
class ProviderDropdown extends React.Component {
    state = {  } 
    render() { 
        return (
            <>
                <p className="single-form-row">
                    <label>Provider: </label>
                    <span className="have-selectbox">
                        <select className="custom-selectbox">
                            <option>EOSIO</option>
                            <option>EOSIO - 1</option>
                            <option>EOSIO - 2</option>
                        </select>
                        <i className="fa fa-angle-down"></i>
                    </span>

                    <span className="have-icon"></span>
                </p>
            </>
        );
    }
}
 
export default ProviderDropdown;
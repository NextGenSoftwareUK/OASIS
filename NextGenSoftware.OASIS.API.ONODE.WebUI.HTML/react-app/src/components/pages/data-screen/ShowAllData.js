import React from 'react';

import "./DataScreen.css";

class ShowAllData extends React.Component {
    render() { 
        return (
            <>
                <div className="data-screen-container">
                    <h2>Data</h2>

                    <div className="table-container">
                        <div className="single-row heading-row">
                            <span>Date</span>
                            <span>Size</span>
                            <span>File</span>
                            <span>Edit</span>
                            <span>View</span>
                            <span>Delete</span>
                        </div>

                        <div className="single-row data-row">
                            <span>11/04/2021</span>
                            <span>22K</span>
                            <span>save</span>
                            <span>edit</span>
                            <span>search</span>
                            <span>delete</span>

                            <div className="form-container">
                                <form>
                                    <p>
                                        <label>Provider: </label>
                                        <select>
                                            <option>EOSIS</option>
                                            <option>EOSIS - 1</option>
                                            <option>EOSIS - 2</option>
                                        </select>

                                        <span className="have-icon"></span>
                                    </p>

                                    <p>
                                        <label>Json: </label>
                                        <textarea></textarea>
                                    </p>

                                    <p>
                                        <label>File: </label>
                                        <input type="file" />
                                    </p>

                                    <p>
                                        <button type="submit">Save</button>
                                    </p>
                                </form>
                            </div>
                        </div>
                    </div>
                </div>
            </>
        );
    }
}
 
export default ShowAllData;
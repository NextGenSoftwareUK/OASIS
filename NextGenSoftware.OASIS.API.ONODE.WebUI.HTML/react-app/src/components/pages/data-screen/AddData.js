import React from 'react';

class AddData extends React.Component {
    render() { 
        return (
            <>
                <div className="data-screen-container">
                    <h2>Data</h2>
                    <h2>Add</h2>

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
            </>
        );
    }
}
 
export default AddData;
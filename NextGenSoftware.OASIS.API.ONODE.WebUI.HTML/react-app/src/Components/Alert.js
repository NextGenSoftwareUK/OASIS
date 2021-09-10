import React from "react";

import "../CSS/Alert.css";

const Alert = ({ type, message }) => (
    <div className={`alert alert--${type}`}>
        <p>{message}</p>
    </div>
);

export default Alert;

import React from "react";

import "../css/Alert.css";

const Alert = ({ type, message }) => (
    <div className={`alert alert--${type}`}>
        <p>{message}</p>
    </div>
);

export default Alert;
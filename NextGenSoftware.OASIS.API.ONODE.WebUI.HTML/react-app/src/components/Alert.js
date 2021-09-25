import React from "react";

import "../assets/scss/alert.scss";

const Alert = ({ type, message }) => (
    <div className={`alert alert--${type}`}>
        <p>{message}</p>
    </div>
);

export default Alert;
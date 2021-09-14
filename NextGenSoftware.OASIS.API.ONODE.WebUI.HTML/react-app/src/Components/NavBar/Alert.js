import React from "react";

const Alert = ({ type, message }) => (
    <div className={`alert alert--${type}`}>
        <p>{message}</p>
    </div>
);

export default Alert;

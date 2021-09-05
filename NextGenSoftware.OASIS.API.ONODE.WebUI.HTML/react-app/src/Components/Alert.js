import React from "react";
import "../CSS/Alert.css";

const Alert = ({ type, message }) => (
  <div class={`alert alert--${type}`}>
    <p>{message}</p>
  </div>
);

export default Alert;

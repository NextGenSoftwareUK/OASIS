
import React from "react";
import Loader from "react-loader-spinner";

import ShowIcon from '../img/visible-icon.svg';
import HideIcon from '../img/hidden-icon.svg';

import Alert from './Alert';
import "../CSS/Login.css";

import axios from "axios";
import { Formik } from "formik";
import * as Yup from "yup";

export default class Signup extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            password: '',
            confirmPassword: '',
            showPassword: false,
            showConfirmPassword: false,
            loading: false,
            alert: null
        }
    }

    initialValues = {
        email: '',
        password: '',
        confirmPassword: ''
    }

    validationSchema = Yup.object().shape({
        email: Yup.string()
            .email("Email is invalid")
            .required("Email is required"),
        password: Yup.string()
            .required("No password provided.")
            .min(8, "Password is too short - should be 8 characters minimum."),
        confirmPassword: Yup.string()
            .required("No password provided.")
            .min(8, "Password is too short - should be 8 characters minimum.")
            .oneOf([Yup.ref('password'), null], "Password did not match")
    })

    // onSignup(event) {
    //     event.preventDefault();
    //     let email = document.getElementById('signup-email').value;
    //     let password = document.getElementById('signup-password').value;
    //     let confirmPassword = document.getElementById('confirm-signup-password').value;
    //     let userObject = {
    //     email,
    //     password,
    //     confirmPassword,
    //     "acceptTerms": true,
    //     "avatarType": "User"
    //     }
    //     const userAction = async () => {
    //     const response = await fetch('https://api.oasisplatform.world/api/avatar/register', {
    //         method: 'POST',
    //         body: JSON.stringify(userObject), // string or object
    //         headers: {
    //         'Content-Type': 'application/json'
    //         }
    //     });
    //     if (response.status === 200) {
    //         const myJson = await response.json(); //extract JSON from the http response
    //         alert(myJson.message);

    //         // hide the login/signup buttons
    //         var elementList = document.getElementsByClassName("nav-logins");
    //         var avatarDropdowm = document.getElementByClassName("nav-avatar-dropdowm");

    //         for (var i = 0; i < elementList.length; i++) {
    //         elementList[i].classList.add('hide-logins')
    //         }
    //         avatarDropdowm.classList.add('enabled')
    //         //===============================//

    //         window.location.reload();
    //     }
    //     else {
    //         const myJson = await response.json(); //extract JSON from the http response
    //         alert(myJson.title);
    //         window.location.reload();
    //     }

    //     }
    //     userAction();
    // }

    handleSignup = () => {
        // e.preventDefault();

        if (this.state.password === this.state.confirmPassword) {
            let data = {
                email: this.state.email,
                password: this.state.password
            }

            const headers = {
                'Content-Type': 'application/json'
            };

            this.setState({ loading: true })
            axios.post('https://api.oasisplatform.world/api/avatar/register', data, { headers })
                .then(response => {
                    console.log(response)
                    this.setState({ loading: false })
                    this.setState({ alert: { type: 'success', text: response.data } });
                    // Remove alert after 5 sec
                    setTimeout(() => this.setState({ alert: null }), 5000)
                    console.log(this.state.alert)
                }).catch(error => {
                    console.error(error.response.data);
                    this.setState({ loading: false })
                    this.setState({ alert: { type: 'error', text: error.response.data.title } })
                    setTimeout(() => this.setState({ alert: null }), 5000)
                });
        } else {
            console.log('Password did not match');
            alert('Password did not match');
        }
    }

    render() {
        const { alert, showPassword, showConfirmPassword } = this.state;

        return (

            <Formik
                initialValues={this.initialValues}
                validationSchema={this.validationSchema}
                onSubmit={(values, { setSubmitting, resetForm }) => {
                    setTimeout(() => {
                        this.setState({
                            email: values.email,
                            password: values.password,
                            confirmPassword: values.confirmPassword
                        })
                        this.handleSignup();

                        setSubmitting(true);
                        // resetForm();
                        setSubmitting(false);
                    }, 400)
                }}
            >
                {({ values, errors, touched, isSubmitting, handleChange, handleBlur, handleSubmit }) => (
                    <form className="login-form" onSubmit={handleSubmit}>
                        {alert ? <Alert message={alert.text} type={alert.type} /> : null}
                        <div className="login-title">
                            <h1 className="login-header">Sign Up</h1>

                            <p className="login-title-text">Already have an account?
                                <span onClick={this.props.change} className="link"> Log In!</span>
                            </p>
                        </div>

                        <div className="login-inputs">

                            <div className={this.handleFormFieldClass(errors.email, touched.email)}>
                                <label htmlFor="login-email">EMAIL</label>
                                <input
                                    type="email"
                                    name="email"
                                    value={values.email}
                                    onChange={handleChange}
                                    onBlur={handleBlur}
                                    placeholder="name@example.com"
                                />
                                <span className="text-danger">{errors.email && touched.email && errors.email}</span>
                            </div>

                            <div className={this.handleFormFieldClass(errors.password, touched.password)}>
                                <label htmlFor="login-password">PASSWORD</label>
                                <div className="have-icon">
                                    <input
                                        type={`${showPassword ? "text" : "password"}`}
                                        name="password"
                                        value={values.password}
                                        onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder="password"
                                    />
                                    <img
                                        className="field-icon"
                                        onClick={() => this.setState({ showPassword: !showPassword })}
                                        src={showPassword ? ShowIcon : HideIcon}
                                        alt="loading..."
                                    />
                                </div>
                                <span className="text-danger">{errors.password && touched.password && errors.password}</span>
                            </div>

                            <div className={this.handleFormFieldClass(errors.confirmPassword, touched.confirmPassword)}>
                                <label htmlFor="login-password">CONFIRM PASSWORD</label>
                                <div className="have-icon">
                                    <input
                                        type={`${showConfirmPassword ? "text" : "password"}`}
                                        name="confirmPassword"
                                        value={values.confirmPassword}
                                        onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder="password"
                                    />
                                    <img
                                        className="field-icon"
                                        onClick={() => this.setState({ showConfirmPassword: !showConfirmPassword })}
                                        src={showConfirmPassword ? ShowIcon : HideIcon}
                                        alt="loading..."
                                    />
                                </div>
                                <span className="text-danger">{errors.confirmPassword && touched.confirmPassword && errors.confirmPassword}</span>
                            </div>

                            {
                                this.state.loading
                                    ?

                                    <button type="submit" disabled className="login-submit">
                                        Logging in <Loader type="Oval" height={15} width={15} color="#fff" />
                                    </button>

                                    :

                                    <button type="submit" className="login-submit" disabled={isSubmitting}>Submit</button>
                            }
                        </div>

                    </form>
                )}
            </Formik>
        )
    }

    handleFormFieldClass(error, touched) {
        let classes = "single-form-field ";
        classes += (error && touched) ? "has-error" : "";

        return classes;
    }
}

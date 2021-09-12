import React from 'react';

import ShowIcon from '../../img/visible-icon.svg';
import HideIcon from '../../img/hidden-icon.svg';

import Alert from './Alert';
import '../../CSS/Login.css';

import Loader from 'react-loader-spinner';
import { Formik } from "formik";
import * as Yup from "yup";
const axios = require('axios');

export default class Login extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            password: '',
            showPassword: false,
            loading: false,
            alert: null
        }
    }

    initialValues = {
        email: '',
        password: ''
    }
    validationSchema = Yup.object().shape({
        email: Yup.string()
            .email('Email is invalid')
            .required("Email is required"),
        password: Yup.string()
            .required("No password provided.")
            .min(8, "Password is too short - should be 8 characters minimum.")
    })

    // onLogin(event) {

    // event.preventDefault();
    // console.log(`${this.state.email}`);
    // let email = document.getElementById('login-email').value;
    // let password = document.getElementById('login-password').value;
    // let userObject = {
    //   email,
    //   password
    // }
    // const userAction = async () => {
    //   console.log('doing api call');

    //   const response = await fetch('https://api.oasisplatform.world/api/avatar/authenticate', {
    //     method: 'POST',
    //     body: JSON.stringify(userObject), // string or object
    //     headers: {
    //       'Content-Type': 'application/json'
    //     }
    //   });
    //   if (response.status === 200) {
    //     const myJson = await response.json(); //extract JSON from the http response
    //     alert(myJson.message);

    //     // hide the login/signup buttons
    //     var elementList = document.getElementsByClassName("nav-logins");
    //     var avatarDropdowm = document.getElementByClassName("nav-avatar-dropdowm");

    //     for (var i = 0; i < elementList.length; i++) {
    //       elementList[i].classList.add('hide-logins')
    //     }
    //     avatarDropdowm.classList.add('enabled')
    //     //===============================//

    //     window.location.reload();
    //   } else {
    //     const myJson = await response.json(); //extract JSON from the http response
    //     alert(myJson.title);
    //     window.location.reload();
    //   }

    // }
    // userAction();
    // }

    // componentDidUpdate(pProps, pState) {
    //     if (pState.loading !== this.state.loading) {
    //         this.setState({loading: this.state.loading})
    //     }
    // }

    handleLogin = () => {
        let data = {
            email: this.state.email,
            password: this.state.password
        }

        const headers = {
            'Content-Type': 'application/json'
        };

        this.setState({ loading: true })

        axios.post('https://api.oasisplatform.world/api/avatar/authenticate', data, { headers })
            .then(response => {
                if (response.data.isError) {
                    this.setState({ alert: { type: 'error', text: response.data.message }, loading: false })
                    return
                }
                //Save to response localstorage
                localStorage.setItem('user', JSON.stringify(response.data.avatar))
                this.setState({ loading: false })
                this.setState({ alert: { type: 'success', text: response.data.message } })
                this.props.setState(response.data.avatar)
                //close form if successful
                this.props.closeForm()
                setTimeout(() => this.setState({ alert: null }), 5000)
            }).catch(error => {
                console.error('There was an error!', error);
                this.setState({ loading: false })
                //Remove pop up after 5 sec
                setTimeout(() => this.setState({ alert: null }), 5000)
            })
    }

    render() {
        const { alert, showPassword } = this.state;

        return (
            <Formik
                initialValues={this.initialValues}
                validationSchema={this.validationSchema}
                onSubmit={(values, { setSubmitting, resetForm }) => {
                    setTimeout(() => {
                        this.setState({
                            email: values.email,
                            password: values.password
                        })
                        this.handleLogin();

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
                            <h1 className="login-header">Log In</h1>

                            <p className="login-title-text">
                                Don't have an account? <span onClick={this.props.change} className="link">Sign Up!</span>
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
                                        alt="icon"
                                    />
                                </div>
                                <span className="text-danger">{errors.password && touched.password && errors.password}</span>
                            </div>

                            <label className="link">Forgot Password?</label>
                            <div>
                                <input type="checkbox" name="remember-login" id="remember-login" />
                                <label htmlFor="remember-login">Remember Me</label>
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

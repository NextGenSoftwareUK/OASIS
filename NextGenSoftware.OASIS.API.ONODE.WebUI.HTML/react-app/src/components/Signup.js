import React from "react";

import ShowIcon from '../assets/images/visible-icon.svg';
import HideIcon from '../assets/images/hidden-icon.svg';

import { ToastContainer, toast } from "react-toastify";
import Loader from "react-loader-spinner";
import { Modal } from 'react-bootstrap';
import axios from "axios";
import { Formik } from "formik";
import * as Yup from "yup";

import "../../src/assets/scss/signup.scss";
import "react-loader-spinner/dist/loader/css/react-spinner-loader.css";

export default class Signup extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            loading: false,
            user_data: {
                firstName: '',
                las_name: '',
                email: '',
                password: '',
                confirmPassword: '',
                acceptTerms: true,
                avatarType: 'User'

            },
            showPassword: false,
            showconfirmPassword: false,
        }
    }

    initialValues = {
        firstName: '',
        lastName: '',
        email: '',
        password: '',
        confirmPassword: '',
        acceptTerms: true
    }

    validationSchema = Yup.object().shape({
        firstName: Yup.string()
            .required("First name is required"),
        lastName: Yup.string()
            .required("Last name is required"),
        email: Yup.string()
            .email("Email is invalid")
            .required("Email is required"),
        password: Yup.string()
            .required("No password provided.")
            .min(8, "Password is too short - should be 8 characters minimum."),
        confirmPassword: Yup.string()
            .required("No password provided.")
            .min(8, "Password is too short - should be 8 characters minimum.")
            .oneOf([Yup.ref('password'), null], "Password did not match"),
        acceptTerms: Yup.boolean()
            .required("acceptTerms is required to be checked")    
    })

    handleSignup = () => {
        if (this.state.user_data.password === this.state.user_data.confirmPassword) {
            const { firstName, lastName, email, password, confirmPassword, acceptTerms } = this.state.user_data;
            let data = {
                firstName: firstName,
                lastName: lastName,
                email: email,
                password: password,
                confirmPassword: confirmPassword,
                acceptTerms: acceptTerms,
                avatarType: 'User'
            }

            const headers = {
                'Content-Type': 'application/json'
            };

            this.setState({ loading: true })
            axios.post('https://api.oasisplatform.world/api/avatar/register', data, { headers })
                .then(response => {
                    this.setState({ loading: false })
                    if(response.data.isError) {
                        toast.error(response.data.message)
                    } else {
                        toast.success("Avatar is created successfully");
                    }
                }).catch(error => {
                    console.log(JSON.parse(error))
                    console.log(error)
                    this.setState({ loading: false })
                    toast.error(error.errors);
                });
        } else {
            toast.error("password did not match")
        }
    }

    render() {
        const { showPassword, showconfirmPassword, loading } = this.state;
        const { show, hide, change } = this.props;

        return (
            <>
                <Loader
                    type="Puff"
                    color="#00BFFF"
                    height={100}
                    width={100}
                    timeout={3000} //3 secs
                />
                <ToastContainer
                    position="top-center"
                    autoClose={5000}
                    hideProgressBar={false}
                    newestOnTop={false}
                    closeOnClick
                    rtl={false}
                    pauseOnFocusLoss
                    draggable
                    pauseOnHover
                />
                <Formik
                    initialValues={this.initialValues}
                    validationSchema={this.validationSchema}
                    onSubmit={(values, { setSubmitting, resetForm }) => {
                        setTimeout(() => {
                            const { firstName, lastName, email, password, confirmPassword, acceptTerms } = values;
                            let user_data = {
                                firstName: firstName,
                                lastName: lastName,
                                email: email,
                                password: password,
                                confirmPassword: confirmPassword,
                                acceptTerms: acceptTerms
                            }
                            this.setState({ user_data })
                            this.handleSignup();

                            setSubmitting(true);
                            // resetForm();
                            setSubmitting(false);
                        }, 400)
                    }}
                >
                    {({ values, errors, touched, isSubmitting, handleChange, handleBlur, handleSubmit }) => (
                        <Modal centered className="custom-modal" show={show} onHide={hide}>
                            <Modal.Body>
                                <span className="form-cross-icon" onClick={hide}>
                                    <i className="fa fa-times"></i>
                                </span>

                                <form className="custom-form" onSubmit={handleSubmit}>
                                    <div className="form-header">
                                        <h2>Sign Up</h2>

                                        <p>
                                            Already have an account? 
                                            <span className="text-link" onClick={change}> Log In!</span>
                                        </p>
                                    </div>

                                    <div className="form-inputs grid-form">
                                        <div className={this.handleFormFieldClass(errors.firstName, touched.firstName)}>
                                            <label>First Name</label>
                                            <input
                                                type="text"
                                                name="firstName"
                                                value={values.firstName}
                                                onChange={handleChange}
                                                onBlur={handleBlur}
                                                placeholder="Jhone Doe"
                                            />
                                            <span className="text-danger">{errors.firstName && touched.firstName && errors.firstName}</span>
                                        </div>

                                        <div className={this.handleFormFieldClass(errors.lastName, touched.lastName)}>
                                            <label>Last Name</label>
                                            <input
                                                type="text"
                                                name="lastName"
                                                value={values.lastName}
                                                onChange={handleChange}
                                                onBlur={handleBlur}
                                                placeholder="Jhone Doe"
                                            />
                                            <span className="text-danger">{errors.lastName && touched.lastName && errors.lastName}</span>
                                        </div>

                                        <div className={`${this.handleFormFieldClass(errors.email, touched.email)} mail-box`}>
                                            <label>Email</label>
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
                                            <label>Password</label>
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
                                        
                                        <div className={this.handleFormFieldClass(errors.confirmPassword, touched.confirmPassword)} >
                                            <label>Confirm Password</label>
                                            <div className="have-icon">
                                                <input
                                                    type={`${showconfirmPassword ? "text" : "password"}`}
                                                    name="confirmPassword"
                                                    value={values.confirmPassword}
                                                    onChange={handleChange}
                                                    onBlur={handleBlur}
                                                    placeholder="confirm password"
                                                />
                                                <img
                                                    className="field-icon"
                                                    onClick={() => this.setState({ showconfirmPassword: !showconfirmPassword })}
                                                    src={showconfirmPassword ? ShowIcon : HideIcon}
                                                    alt="loading..."
                                                />
                                            </div>
                                            <span className="text-danger">{errors.confirmPassword && touched.confirmPassword && errors.confirmPassword}</span>
                                        </div> 
                                        <div className="remember-me">
                                                <label>
                                                    <input 
                                                        type="checkbox"
                                                        name="acceptTerms" 
                                                        value={values.acceptTerms}
                                                        onChange={handleChange}
                                                        id="acceptTerms" 
                                                    />
                                                    Accept Terms
                                                </label>
                                        </div>
                                        <span className="text-danger">{errors.acceptTerms}</span>                                   

                                        <button type="submit" className="submit-button grid-btn" disabled={isSubmitting}>
                                            {loading ? 'Creating Account ' : 'Submit '} {loading ? <Loader type="Oval" height={15} width={15} color="#fff" /> : null}
                                        </button>
                                    </div>
                                </form>
                            </Modal.Body>
                        </Modal>
                    )}
                </Formik>
            </>
        )
    }

    handleFormFieldClass(error, touched) {
        let classes = "single-form-field ";
        classes += (error && touched) ? "has-error" : "";

        return classes;
    }
}

import React from "react";

import ShowIcon from '../assets/images/visible-icon.svg';
import HideIcon from '../assets/images/hidden-icon.svg';

import { toast } from "react-toastify";
import Loader from "react-loader-spinner";
import { Modal } from 'react-bootstrap';
import { Formik } from "formik";
import oasisApi from "oasis-api";
import * as Yup from "yup";

import "../../src/assets/scss/signup.scss";
import "react-loader-spinner/dist/loader/css/react-spinner-loader.css";

export default class Signup extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            loading: false,
            form: {
                firstName: '',
                lastName: '',
                email: '',
                password: '',
                confirmPassword: '',
                acceptTerms: false,
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
        acceptTerms: false
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
            .required("Accept terms is required to be checked")    
    })

    handleSignup = () => {
        if (this.state.form.password === this.state.form.confirmPassword) {
            if(!this.state.form.acceptTerms){
                toast.info('Please accept terms')
                return
            }
            let data = {...this.state.form}

            this.setState({ loading: true })
            const auth = new oasisApi.Auth();
            auth.signup(data)
                .then(response => {
                    console.log(response)
                    console.log(response)
                    
                    if(response.error) {
                        toast.error('Something went wrong!');
                        return;
                    }

                    if(response.data.result.isError) {
                        toast.error(response.data.result.message)
                        return;
                    }
                    
                    this.props.hide()
                    toast.success(response.data.result.message);
                }).catch(error => {
                    console.log(error)
                    this.setState({ loading: false })
                    toast.error(error.data.result.message);
                }).finally(()=>{
                    this.setState({loading: false})
                });
        } else {
            toast.error("Password did not match")
        }
    }

    render() {
        const { showPassword, showconfirmPassword, loading } = this.state;
        const { show, hide, change } = this.props;

        return (
            <>
                <Formik
                    initialValues={this.initialValues}
                    validationSchema={this.validationSchema}
                    onSubmit={(values, { setSubmitting, resetForm }) => {
                        setTimeout(() => {
                            let form = {...values, avatarType: 'User'};
                            this.setState({ form })
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
                                                disabled={loading}
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
                                                disabled={loading}
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
                                                disabled={loading}
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
                                                    disabled={loading}
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
                                                    disabled={loading}
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
                                                        disabled={loading}
                                                        id="acceptTerms"
                                                    />
                                                    Accept Terms
                                                </label>
                                        </div>
                                        <span className="text-danger">{errors.acceptTerms && touched.acceptTerms && errors.acceptTerms}</span>

                                        {loading ? (
                                          <button
                                            type="submit"
                                            className="submit-button"
                                            disabled={true}
                                            style={{
                                              display: "flex",
                                              justifyContent: "space-around",
                                              cursor: "progress",
                                            }}
                                          >
                                            Submitting
                                            <Loader
                                              type="Oval"
                                              height={15}
                                              width={15}
                                              color="#fff"
                                            />
                                          </button>
                                        ) : (
                                          <button
                                            type="submit"
                                            className="submit-button"
                                            disabled={false}
                                          >Submit</button>
                                        )}
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

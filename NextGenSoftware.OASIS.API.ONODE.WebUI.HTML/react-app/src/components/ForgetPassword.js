import React from 'react';

import ShowIcon from '../assets/images/visible-icon.svg';
import HideIcon from '../assets/images/hidden-icon.svg';

import { ToastContainer, toast } from "react-toastify";

import { Modal } from 'react-bootstrap';
import Loader from 'react-loader-spinner';
import { Formik } from "formik";
import * as Yup from "yup";
const axios = require('axios');


export default class ForgetPassword extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            loading: false,
            user: null
        }
    }

    initialValues = {
        email: ''
    }
    validationSchema = Yup.object().shape({
        email: Yup.string()
            .email('Email is invalid')
            .required("Email is required")
    })

    handleLogin = () => {
        let data = {
            email: this.state.email
        }

        const headers = {
            'Content-Type': 'application/json'
        };

        this.setState({ loading: true })

        axios.post('https://api.oasisplatform.world/api/avatar/authenticate', data, { headers })
            .then(response => {
                if (response.data.isError) {
                    toast.error(" Your email or password is invalid!");
                    return
                }
                // localStorage.setItem('user', JSON.stringify(response.data.avatar))
                // localStorage.setItem('credentials', JSON.stringify(data))
                
                // this.setState({loading: false})
                // toast.success(" Successfully Updated!");
                // this.setState({user: response.data.avatar})

                // this.props.setUserStateData(response.data.avatar);

                this.props.hide();
            }).catch(error => {
                console.error('There was an error!', error);
                this.setState({ loading: false })
            })
    }

    render() {
        const { showPassword, loading } = this.state;
        const { show, hide, change } = this.props;

        return (
            <>
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
                            this.setState({
                                email: values.email
                            });
                            this.handleLogin();

                            setSubmitting(true);
                            // resetForm();
                            setSubmitting(false);
                        }, 400);
                    } }
                >
                    {({ values, errors, touched, isSubmitting, handleChange, handleBlur, handleSubmit }) => (

                        <Modal centered className="custom-modal" show={show} onHide={hide}>
                            <Modal.Body>
                                <span className="form-cross-icon" onClick={hide}>
                                    <i className="fa fa-times"></i>
                                </span>

                                <form className="custom-form" onSubmit={handleSubmit}>
                                    <div className="form-header">
                                        <h2>Forget Password</h2>
                                    </div>

                                    <div className="form-inputs">
                                        <div className={this.handleFormFieldClass(errors.email, touched.email)}>
                                            <label>EMAIL</label>
                                            <input
                                                type="email"
                                                name="email"
                                                value={values.email}
                                                onChange={handleChange}
                                                onBlur={handleBlur}
                                                placeholder="name@example.com" />
                                            <span className="text-danger">{errors.email && touched.email && errors.email}</span>
                                        </div>

                                        <button type="submit" className="submit-button" disabled={isSubmitting}>
                                            {loading ? 'Sending Link' : 'Submit '} {loading ? <Loader type="Oval" height={15} width={15} color="#fff" /> : null}
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

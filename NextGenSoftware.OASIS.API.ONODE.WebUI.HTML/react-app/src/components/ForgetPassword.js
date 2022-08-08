import React from 'react';

import { toast } from "react-toastify";

import { Modal } from 'react-bootstrap';
import Loader from 'react-loader-spinner';
import { Formik } from "formik";
import * as Yup from "yup";
import oasisApi from "oasis-api";


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

    handleForgetPassword = () => {
        let data = {
            email: this.state.email
        }
        this.setState({ loading: true })

        const auth = new oasisApi.Auth();
        auth.forgotPassword(data)
            .then(response => {
                this.setState({ loading: false })
                console.log(response)
                if (response?.data?.result.isError) {
                    toast.error(response.data.result.message);
                    return
                }

                toast.success(response?.data?.result.message);
                this.props.hide();
            })
            .catch(error => {
                console.log(error)
                toast.error(error.data.result.message);
                this.setState({ loading: false })
            })
    }
    
    render() {
        const { loading } = this.state;
        const { show, hide } = this.props;

        return (
            <>
                <Formik
                    initialValues={this.initialValues}
                    validationSchema={this.validationSchema}
                    onSubmit={(values, { setSubmitting, resetForm }) => {
                        setTimeout(() => {
                            this.setState({
                                email: values.email
                            });
                            this.handleForgetPassword();

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
                                        <h2>Forget Password?</h2>
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
                                                disabled={loading}
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

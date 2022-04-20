import { Formik } from "formik";
import { useState } from "react";
import { toast, ToastContainer } from "react-toastify";
import { Modal } from "react-bootstrap";
import Loader from "react-loader-spinner";
import * as Yup from "yup";
import axios from "axios";

function ForgotPassword(props) {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false);

  const handleForget = (_email) => {
    let data = JSON.stringify({
      email: _email,
    });
    setLoading(true);

    const config = {
      method: "post",
      url: "https://api.oasisplatform.world/api/avatar/forgot-password",
      headers: {
        "Content-Type": "application/json",
      },
      data: data,
    };
    axios(config)
      .then((res) => {
        if(res.data.isError){
          console.log(res.data)
          toast.error(res.data.message)
        }
        else{
          toast.success("Success");
          props.hide()
        }
      })

      .catch((err) => {
        console.log(err);
      })
      .finally(() => {
        setLoading(false);
      });
  };

  const validationSchema = Yup.object().shape({
    email: Yup.string().email("Email is invalid").required("Email is required"),
  });
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
        enableReinitialize={true}
        initialValues={{ email }}
        validationSchema={validationSchema}
        onSubmit={(values, { setSubmitting, resetForm }) => {
          setTimeout(() => {
            handleForget(values.email);

            setSubmitting(true);
            // resetForm();
            setSubmitting(false);
          }, 400);
        }}
      >
        {({
          values,
          errors,
          touched,
          isSubmitting,
          handleChange,
          handleBlur,
          handleSubmit,
        }) => (
          <Modal className="custom-modal" show={props.show} onHide={props.hide}>
            <Modal.Body>
              <span className="form-cross-icon" onClick={props.hide}>
                <i className="fa fa-times"></i>
              </span>

              <form className="custom-form" onSubmit={handleSubmit}>
                <div className="form-header">
                  <h2>Forgot password</h2>
                  <div className="form-inputs">
                    <div
                      className={`single-form-field ${
                        errors.email && touched.email ? "has-error" : ""
                      }`}
                    >
                      <label>EMAIL</label>
                      <input
                        type="email"
                        name="email"
                        value={email}
                        disabled={loading}
                        onChange={(e)=>setEmail(e.target.value)}
                        onBlur={handleBlur}
                        placeholder="name@example.com"
                      />
                      <span className="text-danger">
                        {errors.email}
                      </span>
                    </div>
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
                        disabled={loading}
                      >Submit</button>
                    )}
                  </div>
                </div>
              </form>
            </Modal.Body>
          </Modal>
        )}
      </Formik>
    </>
  );
}

export default ForgotPassword;

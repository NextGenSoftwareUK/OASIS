import React from "react";
// import axios from "axios";
import { toast } from "react-toastify";
import oasisApi from "oasis-api";

class ForgotPassword extends React.Component {
    state = {  }
    
    componentDidMount = () => {
        this.forgetPasswordUrlSearch();
    }

    forgetPasswordUrlSearch = () => {
        const queryParams = new URLSearchParams(this.props.location.search);
        const email = queryParams.get("email");

        let data = {
            email: email
        }

        const auth = new oasisApi.Auth();
        auth.forgotPassword(data)
            .then(response => {
                console.log(response)
                if(response.data.result.isError) {
                    toast.error(response.data.result.message)
                    return;
                }
                toast.success(response.data.result.message);
                this.props.history.goBack()
            }).catch(error => {
                console.log(error)
                this.setState({ loading: false })
                toast.error(error.data.result.message);
                this.props.history.goBack()
            })
    }
    render() { 
        return (
            <>
                
            </>
        );
    }
}
 
export default ForgotPassword;
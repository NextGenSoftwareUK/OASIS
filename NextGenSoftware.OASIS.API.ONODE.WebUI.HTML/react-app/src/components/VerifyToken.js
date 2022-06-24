import React from "react";
import axios from "axios";
import { toast } from "react-toastify";

class VerifyToken extends React.Component {
    state = {  }
    
    componentDidMount = () => {
        this.verifyToken();
    }

    verifyToken = () => {
        const queryParams = new URLSearchParams(this.props.location.search);
        const token = queryParams.get("token");

        axios.get('https://api.oasisplatform.world/api/avatar/verify-email?token='+token)
            .then(response => {
                console.log(response)
                if(response.data.result.isError) {
                    toast.error(response.data.result.message)
                } else {
                    toast.success(response.data.result.message)
                }
                this.props.history.goBack()
                console.log(this.props) 
            })
            .catch(error => {
                console.log(error)
            })
    }
    render() { 
        return (
            <>
                
            </>
        );
    }
}
 
export default VerifyToken;
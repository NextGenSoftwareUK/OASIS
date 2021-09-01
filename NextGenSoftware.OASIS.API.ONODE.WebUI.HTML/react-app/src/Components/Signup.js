import React, { Component } from 'react'
import '../CSS/Login.css'

export default class Signup extends Component {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            password: '',
            confirm_password: ''
        }
    }

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

    handleSignup = (e) => {
        e.preventDefault();

        if(this.state.password == this.state.confirm_password) {
            let data = {
                email: this.state.email,
                password: this.state.password
            }
    
            const headers = { 
                'Content-Type': 'application/json'
            };
    
            axios.post('https://api.oasisplatform.world/api/avatar/register', data, {headers})
            .then(response => {
                console.log(response);
            }).catch(error => {
                console.error('There was an error!', error);
            });
        } else {
            console.log('Password did not match');
            alert('Password did not match');
        }
    }

    handleEmailChange = (event) => {
        this.setState({email: event.target.value});
    }

    handlePasswordChange = (event) => {
        this.setState({password: event.target.value});
    }

    handleConfirmPasswordChange = (event) => {
        this.setState({password: event.target.value});
    }

    render() {
        return (
            <form className="login-form" onSubmit={this.handleSignup}>
                <div className="login-title">
                    <h1 className="login-header">Sign Up</h1>
                    <p className="login-title-text">Already have an account?
                        <span onClick={this.props.change} className="link">Log In!</span>
                    </p>
                </div>

                <div className="login-inputs">
                    <label htmlFor="login-email">EMAIL</label>
                    <input value={this.state.email} onChange={this.handleEmailChange} type="email" placeholder="name@example.com" />
            
                    <label htmlFor="login-password">PASSWORD</label>
                    <input type="password" value={this.state.password} onChange={this.handlePasswordChange} />
                
                    <label htmlFor="confirm-signup-password">Confirm Password</label>
                    <input type="password" value={this.state.confirm_password} onChange={this.handleConfirmPasswordChange} />
                
                    <div>
                        <input type="checkbox" name="accept-terms" id="accept-terms" />
                        <label for="accept-terms">
                            I have read and accept the 
                            <a href="#0" className="link">Terms of Service</a>
                        </label>
                    </div>

                    <button type="submit" className="login-submit">Submit</button>
                </div>
            </form>
        )
    }
}

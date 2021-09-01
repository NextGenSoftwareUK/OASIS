import React from 'react';
import '../CSS/Login.css';
const axios = require('axios');

export default class Login extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            password: ''
        }
    }

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
    
    handleLogin = (e) => {
        e.preventDefault();

        let data = {
            email: this.state.email,
            password: this.state.password
        }

        const headers = { 
            'Content-Type': 'application/json'
        };

        axios.post('https://api.oasisplatform.world/api/avatar/authenticate', data, {headers})
        .then(response => {
            console.log(response);
        }).catch(error => {
            console.error('There was an error!', error);
        });
    }

    handleEmailChange = (event) => {
        this.setState({email: event.target.value});
    }

    handlePasswordChange = (event) => {
        this.setState({password: event.target.value});
    }

    render() {
        return (
            <form className="login-form" onSubmit={this.handleLogin}>
                <div className="login-title">
                    <h1 className="login-header">Log In</h1>

                    <p className="login-title-text">
                        Don't have an account? <span onClick={this.props.change} className="link">Sign Up!</span>
                    </p>
                </div>

                <div className="login-inputs">
                    <label htmlFor="login-email">EMAIL</label>
                    <input value={this.state.email} onChange={this.handleEmailChange} type="email" placeholder="name@example.com" />
            
                    <label htmlFor="login-password">PASSWORD</label>
                    <input type="password" value={this.state.password} onChange={this.handlePasswordChange} />
                
                    <label className="link">Forgot Password?</label>
                    <div>
                        <input type="checkbox" name="remember-login" />
                        <label htmlFor="remember-login">Remember Me</label>
                    </div>

                    <button type="submit" className="login-submit">Submit</button>
                </div>
            </form>
        )
    }
}

// File: signup.js

function onSignup() {
    // let title = document.getElementById('signup-title').value;
    // let firstName = document.getElementById('signup-first-name').value;
    // let lastName = document.getElementById('signup-last-name').value;
    let email = document.getElementById('signup-email').value;
    let password = document.getElementById('signup-password').value;
    let confirmPassword = document.getElementById('confirm-signup-password').value;
    let userObject = {
        // title,
        // firstName,
        // lastName,
        email,
        password,
        confirmPassword,
        "acceptTerms": true,
        "avatarType": "User"
    }
    const userAction = async () => {
        const response = await fetch('https://api.oasisplatform.world/api/avatar/register', {
          method: 'POST',
          body: JSON.stringify(userObject), // string or object
          headers: {
            'Content-Type': 'application/json'
          }
        });
        if(response.status === 200)
        {
            const myJson = await response.json(); //extract JSON from the http response
            alert(myJson.message);
            window.location.reload();
        }
        else
        {
            const myJson = await response.json(); //extract JSON from the http response
            alert(myJson.title);
            window.location.reload();
        }

    }
    userAction();
}

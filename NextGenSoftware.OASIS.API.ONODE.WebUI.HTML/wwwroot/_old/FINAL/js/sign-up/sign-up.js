function onSignup(){
    let title = document.getElementById('input-title').value;
    let firstName = document.getElementById('input-first-name').value;
    let lastName = document.getElementById('input-last-name').value;
    let email = document.getElementById('input-email').value;
    let password = document.getElementById('input-password').value;
    let confirmPassword = document.getElementById('input-confirm-password').value;
    let userObject = {
        title,
        firstName,
        lastName,
        email,
        password,
        confirmPassword,
        "acceptTerms": true,
        "avatarType": "User"
    }
    const userAction = async () => {
        const response = await fetch('https://staging.api.oasisplatform.world/api/avatar/register', {
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

function addAuthPopup(login, msg, e) {
    // Get and remove previous pop ups
    var prev = document.getElementsByClassName('alert')[0];
    if (prev) prev.remove();
    console.log(msg);
    var formId;
    var type;
    var alert = msg.message || msg.title;
    if (msg.isError || msg.status === 400) {
        type = 'error';
        // alert = msg.title;
    } else {
        type = 'success';
        if (login) {
            localStorage.setItem('avatar', JSON.stringify(msg.avatar));
            localStorage.setItem('loggedIn', true);
        }
        //Reloads page after 5sec
        setTimeout(() => window.location.reload(), 5000);
    }
    login ? (formId = 'login-form') : (formId = 'signup-form');
    // Create popup element
    let target = document.getElementById(formId);
    var div = document.createElement('div');
    div.classList.add('alert');
    div.classList.add(type);
    div.innerHTML = alert;
    target.parentNode.insertBefore(div, target);
    console.log(type);
    e.preventDefault();
}
function onLogin() {
    // Get button and change it when pressed
    const submitBtn = document.getElementById('login-submit');
    submitBtn.innerHTML =
        'logging in... <img width="20px" src="assets/img/loading.gif"/>';
    submitBtn.disabled = true;
    let n = {
        email: document.getElementById('login-email').value,
        password: document.getElementById('login-password').value,
    };
    (async () => {
        const e = await fetch(
            'https://api.oasisweb4.one/api/avatar/authenticate',
            {
                method: 'POST',
                body: JSON.stringify(n),
                headers: { 'Content-Type': 'application/json' },
            }
        );
        // Re-enable button after request
        submitBtn.innerHTML = 'Submit';
        submitBtn.disabled = false;
        var t;
        200 === e.status
            ? ((t = await e.json()), addAuthPopup(true, t, e))
            : (submitBtn.classList.add('error'),
              (t = await e.json()),
              addAuthPopup(true, t, e)),
            window.location.reload();
    })();
}

async function onLogout() {
    const user = JSON.parse(localStorage.getItem('avatar'));
    console.log(JSON.parse(localStorage.getItem('avatar')));
    const body = { token: user.jwtToken };
    const loading = document.getElementById('loading');

    loading.classList.add('modal');
    loading.classList.add('is-visible');
    loading.innerHTML = '<img src="assets/img/loading.gif"/>';
    console.log(body);

    const e = await fetch(
        'https://api.oasisweb4.one/api/avatar/revoke-token',
        {
            method: 'POST',
            body: JSON.stringify(body),
            headers: {
                'Content-Type': 'application/json',
            },
        }
    );
    localStorage.removeItem('avatar');
    localStorage.setItem('loggedIn', false);
    window.location.reload();
}

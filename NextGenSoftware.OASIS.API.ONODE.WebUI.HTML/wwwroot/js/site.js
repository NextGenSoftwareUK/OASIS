////import oasis from "web4-oasis-api"
////import { Auth } from "web4-oasis-api";

////const oasisAuth = new Auth();
const remoteResponse = document.getElementById('remoteResponse');
const remoteHost = document.getElementByName('remoteHost');
const username = document.getElementByName('username');
const password = document.getElementByName('password');

function FetchRemote(remoteHost, path, httpMethod) {
    //const remoteResponse = document.getElementById('remoteResponse');

    fetch(`${remoteHost}${path}`,
        {
            method: httpMethod,
        }).then(response => {
            if (response.ok) {
                response.text().then(text => {
                    remoteResponse.innerText = text;
                });
            }
            else {
                remoteResponse.innerText = response.status;
            }
        })
        .catch(() => remoteResponse.innerText = 'An error occurred, might be CORS?! :) Press F12 to open the web debug tools');
}

function Login(remoteHost, username, password)
{
    sendData(remoteHost, '/api/avatar/authenticate', { username: username, password: password });
}

function LoginUsingForm()
{
    sendData(remoteHost, '/api/avatar/authenticate', { username: username, password: password });
}

function sendData(remoteHost, path, data) {
    console.log('Sending data');
    console.log('remoteHost:' + remoteHost);
    console.log('data:' + data);


    const XHR = new XMLHttpRequest();

    const urlEncodedDataPairs = [];

    // Turn the data object into an array of URL-encoded key/value pairs.
    for (const [name, value] of Object.entries(data)) {
        urlEncodedDataPairs.push(`${encodeURIComponent(name)}=${encodeURIComponent(value)}`);
    }

    // Combine the pairs into a single string and replace all %-encoded spaces to
    // the '+' character; matches the behavior of browser form submissions.
    const urlEncodedData = urlEncodedDataPairs.join('&').replace(/%20/g, '+');

    // Define what happens on successful data submission
    XHR.addEventListener('load', (event) => {
        alert('SUCCESS');
        remoteResponse.innerText = XHR.response;
    });

    // Define what happens in case of error
XHR.addEventListener('error', (event) => {
        alert('ERROR');
        remoteResponse.innerText = XHR.response;
    });

    // Set up our request
    XHR.open('POST', `${remoteHost}${path}`);

    // Add the required HTTP header for form data POST requests
    XHR.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');

    // Finally, send our data.
    XHR.send(urlEncodedData);
}


//function LoginUsingNPMPackage(username, password) {

//    oasisAuth.login({
//        username: username,
//        password: password,
//    }).then((res) => {
//        if (res.error) {
//            remoteResponse.innerText = res.error;
//            console.log(res.error);
//        }
//        else {
//            remoteResponse.innerText = res;
//            console.log(res);
//        }
//    }).catch((err) => {
//        console.log(err)
//    })
//}
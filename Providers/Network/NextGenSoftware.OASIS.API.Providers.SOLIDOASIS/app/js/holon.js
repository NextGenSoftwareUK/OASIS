import {
    getSolidDataset,
    getThing,
    setThing,
    getStringNoLocale,
    setStringNoLocale,
    saveSolidDatasetAt
} from "@inrupt/solid-client";
import { Session } from "@inrupt/solid-client-authn-browser";
import { VCARD } from "@inrupt/vocab-common-rdf";

const SOLID_IDENTITY_PROVIDER = "https://solidcommunity.net";
document.getElementById(
    "solid_identity_provider"
).innerHTML = `[<a target="_blank" href="${SOLID_IDENTITY_PROVIDER}">${SOLID_IDENTITY_PROVIDER}</a>]`;

const session = new Session();

const buttonLogin = document.getElementById("btnLogin");
const buttonCreate = document.getElementById("btnCreate");

async function login() {
    if (!session.info.isLoggedIn) {
        await session.login({
            oidcIssuer: SOLID_IDENTITY_PROVIDER,
            clientName: "Inrupt SOLID OASIS client app",
            redirectUrl: window.location.href
        });
    }
}

async function handleRedirectAfterLogin() {
    await session.handleIncomingRedirect(window.location.href);
    if (session.info.isLoggedIn) {
        document.getElementById(
            "labelStatus"
        ).innerHTML = `Your session is logged in with the WebID [<a target="_blank" href="${session.info.webId}">${session.info.webId}</a>].`;
        document.getElementById("webId").textContent = session.info.webId;
    }
}

async function writeProfile() {

    const name = document.getElementById("name").value;
    const holonType = document.getElementById("holonType").value;
    const version = document.getElementById("version").value;
    const description = document.getElementById("description").value;

    const holon = 
    {
        holonName: name,
        holonType: holonType,
        holonVersion: version,
        holonDesc: description
    };

    const holonJsonStr = JSON.stringify(holon);

    if (!session.info.isLoggedIn) {
        // You must be authenticated to write.
        document.getElementById(
            "labelStatus"
        ).textContent = `...you can't write until you first login!`;
        return;
    }

    const webID = session.info.webId;

    const profileDocumentUrl = new URL(webID);
    profileDocumentUrl.hash = "";

    let myProfileDataset = await getSolidDataset(profileDocumentUrl.href, {
        fetch: session.fetch
    });

    let profile = getThing(myProfileDataset, webID);

    profile = setStringNoLocale(profile, VCARD.fn, holonJsonStr);

    myProfileDataset = setThing(myProfileDataset, profile);

    await saveSolidDatasetAt(profileDocumentUrl.href, myProfileDataset, {
        fetch: session.fetch
    });

    document.getElementById("nameRow").textContent = name;
    document.getElementById("holonTypeRow").textContent = holonType;
    document.getElementById("versionRow").textContent = version;
    document.getElementById("descriptionRow").textContent = description;
}

async function readProfile() {
    const profileDocumentUrl = new URL(session.info.webId);
    profileDocumentUrl.hash = "";

    let myDataset;
    try {
        if (session.info.isLoggedIn) {
            myDataset = await getSolidDataset(profileDocumentUrl.href, { fetch: session.fetch });
        } else {
            myDataset = await getSolidDataset(profileDocumentUrl.href);
        }
    } catch (error) {
        document.getElementById(
            "labelStatus"
        ).textContent = `Error: [${error}]`;
        return false;
    }

    const profile = getThing(myDataset, webID);
    const formatted = getStringNoLocale(profile, VCARD.fn);

    const holonParsed = JSON.parse(formatted);

    document.getElementById("nameRow").textContent = holonParsed.holonName;
    document.getElementById("holonTypeRow").textContent = holonParsed.holonType;
    document.getElementById("versionRow").textContent = holonParsed.holonVersion;
    document.getElementById("descriptionRow").textContent = holonParsed.holonDesc;
}

handleRedirectAfterLogin();

buttonLogin.onclick = async function () {
    await login();
    readProfile();
};

buttonCreate.onclick = function () {
    writeProfile();
};
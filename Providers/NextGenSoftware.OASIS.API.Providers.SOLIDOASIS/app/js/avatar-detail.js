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
    const userName = document.getElementById("username").value;
    const email = document.getElementById("email").value;
    const address = document.getElementById("address").value;
    const country = document.getElementById("country").value;
    const county = document.getElementById("county").value;
    const postcode = document.getElementById("postcode").value;
    const town = document.getElementById("town").value;
    const level = document.getElementById("level").value;
    const xp = document.getElementById("xp").value;
    const holonType = document.getElementById("holonType").value;
    const version = document.getElementById("version").value;
    const description = document.getElementById("description").value;

    const entity = 
    {
        entityName: name,
        entityAddress: address,
        entityCountry: country,
        entityCounty: county,
        entityPostcode: postcode,
        entityTown: town,
        entityLevel: level,
        entityXp: xp,
        entityUserName: userName,
        entityEmail: email,
        entityType: holonType,
        entityVersion: version,
        entityDesc: description
    };

    const entityJsonStr = JSON.stringify(entity);

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

    profile = setStringNoLocale(profile, VCARD.fn, entityJsonStr);

    myProfileDataset = setThing(myProfileDataset, profile);

    await saveSolidDatasetAt(profileDocumentUrl.href, myProfileDataset, {
        fetch: session.fetch
    });

    document.getElementById("nameRow").textContent = entity.entityName;
    document.getElementById("usernameRow").textContent = entity.entityUserName;
    document.getElementById("emailRow").textContent = entity.entityEmail;
    document.getElementById("addressRow").textContent = entity.entityAddress;
    document.getElementById("countryRow").textContent = entity.entityCountry;
    document.getElementById("countyRow").textContent = entity.entityCounty;
    document.getElementById("postcodeRow").textContent = entity.entityPostcode;
    document.getElementById("townRow").textContent = entity.entityTown;
    document.getElementById("levelRow").textContent = entity.entityLevel;
    document.getElementById("xpRow").textContent = entity.entityXp;
    document.getElementById("holonTypeRow").textContent = entity.entityType;
    document.getElementById("versionRow").textContent = entity.entityVersion;
    document.getElementById("descRow").textContent = entity.entityDesc;
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

    const entityParsed = JSON.parse(formatted);

    document.getElementById("nameRow").textContent = entityParsed.entityName;
    document.getElementById("usernameRow").textContent = entityParsed.entityUserName;
    document.getElementById("emailRow").textContent = entityParsed.entityEmail;
    document.getElementById("addressRow").textContent = entityParsed.entityAddress;
    document.getElementById("countryRow").textContent = entityParsed.entityCountry;
    document.getElementById("countyRow").textContent = entityParsed.entityCounty;
    document.getElementById("postcodeRow").textContent = entityParsed.entityPostcode;
    document.getElementById("townRow").textContent = entityParsed.entityTown;
    document.getElementById("levelRow").textContent = entityParsed.entityLevel;
    document.getElementById("xpRow").textContent = entityParsed.entityXp;
    document.getElementById("holonTypeRow").textContent = entityParsed.entityType;
    document.getElementById("versionRow").textContent = entityParsed.entityVersion;
    document.getElementById("descRow").textContent = entityParsed.entityDesc;
}

handleRedirectAfterLogin();

buttonLogin.onclick = async function () {
    await login();
    readProfile();
};

buttonCreate.onclick = function () {
    writeProfile();
};
import axios from "axios";

export const getUserById = (id, token) => {
    var config = {
        method: "get",
        url: `https://api.oasisplatform.world/api/avatar/GetById/${id}`,
        headers: {
            Authorization: `Bearer ${token.jwt}`,
            Cookie: `refreshToken=${token.refresh}`,
        },
    };

    return axios(config)
        .then((response) => {
            return response.data;
        })
        .catch((error) => {
            console.log("error");
            console.log(error);
        });
};

export const extractKarma = async (users, token) => {
    let karmaRecord = [];
    for (let i = 0; i <= users.length - 1; i++) {
        const user = users[i];
        if (user.karmaAkashicRecords) {
            let userKarmas = user.karmaAkashicRecords;
            for (let j = 0; j <= userKarmas.length - 1; j++) {
                let userKarma = userKarmas[j];
                const username = await getUserById(userKarma.avatarId, token);
                console.log(username);
                let temp = {
                    date: userKarma.date,
                    avatar: username,
                    karma: userKarma.karma,
                    title: userKarma.karmaSourceTitle,
                    description: userKarma.karmaSourceDesc,
                    posNeg:
                        userKarma.karmaTypePositive.name === "None"
                            ? "Negative"
                            : "Positive",
                    type:
                        userKarma.karmaTypePositive.name === "None"
                            ? userKarma.karmaTypeNegative.name
                            : userKarma.karmaTypePositive.name,
                    source: userKarma.karmaSource.name,
                    link: userKarma.provider.name,
                };
                // console.log(temp)
                karmaRecord.push(temp);
            }
        }
    }
    return karmaRecord;
};

export const login = (credentials) => {
    const headers = {
        "Content-Type": "application/json",
    };

    return axios
        .post(
            "https://api.oasisplatform.world/api/avatar/authenticate",
            credentials,
            { headers }
        )
        .then((response) => {
            if (response.data.isError) {
                return -1;
            }
            //Save to response localstorage
            localStorage.setItem("user", JSON.stringify(response.data.avatar));
            return response.data.avatar;
        })
        .catch((error) => {
            return -1;
        });
};

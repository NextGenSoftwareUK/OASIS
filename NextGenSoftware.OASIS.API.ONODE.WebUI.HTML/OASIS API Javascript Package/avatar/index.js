import axios from "axios";
import Auth from "../auth/auth";

class Avatar {
  constructor() {
    if (localStorage.getItem("user")) {
      let user = JSON.parse(localStorage.getItem("user")).user;
      this.token = { jwtToken: user.jwtToken, refresh: user.refreshToken };
    }
    this.callLogin();
  }
  async callLogin() {
    const credentials = JSON.parse(localStorage.getItem("login"));
    const auth = new Auth();
    await auth.login(credentials);
  }

  getAll(token = {}) {
    this.callLogin();
    if (this.token) {
      token = this.token;
    }

    const config = {
      method: "get",
      url: "https://api.oasisplatform.world/api/avatar/GetAll",
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
      },
    };

    return axios(config)
      .then(function (response) {
        return { error: false, data: response.data };
      })

      .catch(function (error) {
        console.log(error);
        return { error: true, data: error };
      });
  }

  get(id) {
    this.callLogin();
    let token = this.token;

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/avatar/GetById/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
      },
    };

    return axios(config)
      .then(function (response) {
        return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }
}

export default Avatar;

//ts-check
import axios from "axios";
import Auth from "../auth/auth.js";

class Avatar {
  constructor() {
    if (localStorage.getItem("user")) {
      let user = JSON.parse(localStorage.getItem("user"));
      this.token = { jwtToken: user.jwtToken, refresh: user.refreshToken };
    }
    this.callLogin();
  }
  async callLogin(obj = this) {
    const credentials = JSON.parse(localStorage.getItem("login"));
    const auth = new Auth();
    await auth.login(credentials);
    let user = JSON.parse(localStorage.getItem("user"));
    obj.token = { jwtToken: user.jwtToken, refresh: user.refreshToken };
    console.log(obj);
    return obj.token;
  }

  async getAll(token = {}) {
    await this.callLogin();
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

  async get(id) {
    await this.callLogin();
    let token = this.token;

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/avatar/GetById/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
        "Content-Type": "application/json",
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

  async update(id, data) {
    await this.callLogin();

    data = JSON.stringify(data);

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/avatar/Update/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
        "Content-Type": "application/json",
      },
      data,
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

  async delete(id) {
    await this.callLogin();
    const config = {
      method: "delete",
      url: `https://api.oasisplatform.world/api/avatar/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
        "Content-Type": "application/json",
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

  async addKarma(
    id = "",
    data = {
      karmaType,
      karmaSourceType,
      karmaSourceTitle,
      karmaSourceDesc,
    }
  ) {
    await this.callLogin();

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/avatar/AddKarmaToAvatar/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
        "Content-Type": "application/json",
      },
      data,
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

  async removeKarma(
    id = "",
    data = {
      karmaType,
      karmaSourceType,
      karmaSourceTitle,
      karmaSourceDesc,
    }
  ) {
    await this.callLogin();
    data = JSON.stringify(data);

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/avatar/RemoveKarmaFromAvatar/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
        "Content-Type": "application/json",
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
}

export default Avatar;

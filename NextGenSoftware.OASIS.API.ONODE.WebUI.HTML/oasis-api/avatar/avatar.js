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
    if (JSON.parse(localStorage.getItem("login"))) {
      const credentials = JSON.parse(localStorage.getItem("login"));
      const auth = new Auth();
      const ret = await auth.login(credentials);
      if (ret.error) return -1;
      let user = JSON.parse(localStorage.getItem("user"));
      obj.token = { jwtToken: user.jwtToken, refresh: user.refreshToken };
      console.log(obj);
      return obj.token;
    } else return -1;
  }

  async getAll(token = {}) {
    const temp = await this.callLogin();
    if (temp === -1) {
      return { error: true, data: { message: "You are not logged in!" } };
    } else if (this.token) {
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
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: response.data };
      })

      .catch(function (error) {
        console.log(error);
        return { error: true, data: error };
      });
  }

  async get(id) {
    const temp = await this.callLogin();
    let token;
    if (temp === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    else token = temp;

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
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }

  async update(id, data) {
    let temp = await this.callLogin();
    if (temp === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    else token = temp;

    data = JSON.stringify(data);

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/avatar/Update/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        Cookie: `refreshToken=${token.refresh}`,
        "Content-Type": "application/json",
      },
      data,
    };

    return axios(config)
      .then(function (response) {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: response.data };
      })

      .catch(function (error) {
        console.log(error);
        return { error: true, data: error };
      });
  }

  async delete(id) {
    let temp = await this.callLogin();
    if (temp === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    else token = temp;
    const config = {
      method: "delete",
      url: `https://api.oasisplatform.world/api/avatar/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        "Content-Type": "application/json",
      },
    };

    return axios(config)
      .then(function (response) {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
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
    let temp = await this.callLogin();
    if (temp === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    else token = temp;

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/avatar/AddKarmaToAvatar/${id}`,
      headers: {
        Authorization: `Bearer ${token.jwtToken}`,
        "Content-Type": "application/json",
      },
      data,
    };
    return axios(config)
      .then(function (response) {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
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
    let temp = await this.callLogin();
    if (temp === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    else token = temp;
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
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: response.data };
      })

      .catch(function (error) {
        console.log(error);
        return { error: true, data: error };
      });
  }
}

export default Avatar;

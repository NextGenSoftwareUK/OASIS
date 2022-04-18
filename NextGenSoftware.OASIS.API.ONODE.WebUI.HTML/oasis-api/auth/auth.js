import axios from "axios";

class Auth {
  constructor() {
    if (localStorage.getItem("user")) {
      let user = localStorage.getItem("user");
      this.token = {
        jwtToken: user.jwtToken,
        refreshToken: user.refreshToken,
      };
    }
  }

  getUser() {
    if (localStorage.getItem("user")) {
      const user = JSON.parse(localStorage.getItem("user"));
      return { error: false, data: user };
    }
    return { error: true, data: { message: "You are not logged in" } };
  }
  signup(
    data = {
      title: "Mr",
      firstName,
      lastName,
      email,
      password,
      confirmPassword,
      acceptTerms: true,
      avatarType: "User",
    }
  ) {
    data = JSON.stringify(data);
    const config = {
      method: "post",
      url: "https://api.oasisplatform.world/api/avatar/register",
      headers: {
        "Content-Type": "application/json",
      },
      data: data,
    };

    return axios(config)
      .then((res) => {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: res.data };
      })
      .catch((err) => {
        return { error: true, data: err };
      });
  }

  login(
    data = {
      username,
      password,
    }
  ) {
    data = JSON.stringify(data);
    let config = {
      method: "post",
      url: "https://api.oasisplatform.world/api/avatar/authenticate/",
      headers: {
        "Content-Type": "application/json",
      },
      data,
    };

    return axios(config)
      .then((res) => {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        const sto = JSON.stringify(res.data.result.avatar);
        localStorage.setItem("user", sto);
        localStorage.setItem("login", data);
        console.log(sto);
        this.token = {
          jwtToken: res.data.result.avatar.jwtToken,
          refreshToken: res.data.result.avatar.refreshToken,
        };
        return { error: false, data: res.data };
      })
      .catch((err) => {
        return { error: true, data: err };
      });
  }

  logout() {
    if (this.token) {
      const data = { token: JSON.stringify(this.token.jwtToken) };
      var config = {
        method: "post",
        url: "https://api.oasisplatform.world/api/avatar/revoke-token",
        headers: {
          Authorization: `Bearer ${token.jwtToken}`,
          "Content-Type": "application/json",
          Cookie: `refreshToken=${token.refresh}`,
        },
        data: data,
      };

      localStorage.removeItem("user");
      localStorage.removeItem("login");

      return axios(config)
        .then((res) => {
          return { error: false, data: res.data };
        })
        .catch((err) => {
          return { err: false };
        });
    } else return { err: true, message: "Please login first" };
  }

  forgotPassword({ email }) {
    let data = JSON.stringify({
      email,
    });

    const config = {
      method: "post",
      url: "https://api.oasisplatform.world/api/avatar/forgot-password",
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
        "Content-Type": "application/json",
        Cookie: `refreshToken=${this.token.refresh}`,
      },
      data: data,
    };

    return axios(config)
      .then((response) => {
        if (res.data.isError) {
          return { error: true, data: res.data };
        }
        return { error: false, data: response.data };
      })
      .catch((error) => {
        return { error: true, data: error };
      });
  }
}
export default Auth;

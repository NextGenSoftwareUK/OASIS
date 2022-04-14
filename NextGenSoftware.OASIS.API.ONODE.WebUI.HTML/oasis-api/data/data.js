import axios from "axios";
import Avatar from "../avatar/avatar";

class Data {
  constructor() {
    this.avatar = new Avatar();
  }

  async loadHolon(id = "") {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/data/LoadHolon/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }

  async loadAllHolons() {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/data/LoadAllHolons`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }

  async saveHolon(data) {
    data = JSON.stringify(data);
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/data/SaveHolon`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
      data,
    };

    return axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }

  async deleteHolon(id) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    const config = {
      method: "delete",
      url: `https://api.oasisplatform.world/api/data/DeleteHolon/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }
}

export default Data;

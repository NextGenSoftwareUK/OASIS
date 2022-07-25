import axios from "axios";
import Avatar from "../avatar/avatar";

class Karma {
  constructor() {
    this.avatar = new Avatar();
  }

  async getKarmaForAvatar(id) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/karma/GetKarmaForAvatar/${id}`,
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

  async removeKarmaFromAvatar(
    id,
    data = {
      karmaType: "",
      karmaSourceType: "",
      karamSourceTitle: "",
      karmaSourceDesc: "",
    }
  ) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };
    data = JSON.stringify(data);

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/karma/RemoveKarmaFromAvatar/${id}`,
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

  async getKarmaAkashicRecordsForAvatar(id) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/karma/GetKarmaAkashicRecordsForAvatar/${id}`,
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

export default Karma;

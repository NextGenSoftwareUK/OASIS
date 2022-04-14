import axios from "axios";
import Avatar from "../avatar/avatar";

class Holochain {
  constructor() {
    this.avatar = new Avatar();
    this.url = `https://api.oasisplatform.world/api/holochain`;
  }

  _returnState(config) {
    return axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: false, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }

  async getHolochainAgentIdForAvatar(avatarId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === -1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `${this.url}/GetHolochainAgentIdForAvatar?avatarId=${avatarId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }

  async getHolochainAgentPrivateKeyForAvatar(avatarId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `${this.url}/GetHolochainAgentPrivateKeyForAvatar?avatarId=${avatarId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }

  async getAvatarIdForHolochainAgentId(agentId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `${this.url}/GetAvatarIdForHolochainAgentId?agentId=${agentId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }

  async getAvatarForHolochainAgentId(agentId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `${this.url}/GetAvatarForHolochainAgentId?agentId=${agentId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }

  async getHoloFuelBalanceForAgentId(agentId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "get",
      url: `${this.url}/GetHoloFuelBalanceForAgentId?agentId=${agentId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }

  async getHoloFuelBalanceForAvatar(avatarId = "") {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };
    const config = {
      method: "get",
      url: `${this.url}/GetHoloFuelBalanceForAvatar?avatarId=${avatarId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };
    return this._returnState(config);
  }

  async linkHolochainAgentIdToAvatar(data = { agentId: "", avatarId: "" }) {
    this.token = await this.avatar.callLogin();

    if (this.token === 1)
      return { error: true, data: { message: "You are not logged in!" } };

    const config = {
      method: "post",
      url: `${this.url}/${data.avatarId}/${data.agentId}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return this._returnState(config);
  }
}

export default Holochain;

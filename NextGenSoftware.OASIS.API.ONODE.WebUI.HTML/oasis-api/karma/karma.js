import axios from "axios";
import Avatar from "../avatar/avatar";

class Karma {
  constructor() {
    this.avatar = new Avatar();
  }

  async getKarmaForAvatar(id) {
    this.token = await this.avatar.callLogin();

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/karma/GetKarmaForAvatar/${id}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    axios(config)
      .then(function (response) {
        if (response.data.isError) return { error: true, data: response.data };
        else return { error: true, data: response.data };
      })
      .catch(function (error) {
        return { error: true, data: error };
      });
  }
}

export default Karma;

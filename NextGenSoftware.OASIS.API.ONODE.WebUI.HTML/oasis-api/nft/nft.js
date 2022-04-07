import axios from "axios";
import Avatar from "../avatar/avatar";

class NFT {
  constructor() {
    this.avatar = new Avatar();
  }

  async createPurchase(
    data = {
      nftProvider: 1,
      solanaExchange: {
        fromAccount: {
          publicKey: "string",
        },
        toAccount: {
          publicKey: "string",
        },
        memoText: "string",
        amount: 0,
        mintAccount: {
          publicKey: "string",
        },
      },
      cargoExchange: {
        saleId: "string",
      },
    }
  ) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1) {
      return { error: true, data: { message: "You are not logged in!" } };
    }
    data = JSON.stringify(data);

    const config = {
      method: "post",
      url: `https://api.oasisplatform.world/api/Nft/CreatePurchase`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
      data,
    };

    return axios(config)
      .then((response) => {
        if (response.data.isError) {
          return { error: true, data: response.data };
        }
        return { error: false, data: response.data };
      })
      .catch((err) => {
        return { error: true, data: err };
      });
  }

  async getOlandPrice(count = 0, couponCode = 0) {
    this.token = await this.avatar.callLogin();
    if (this.token === -1) {
      return { error: true, data: { message: "You are not logged in!" } };
    }

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/Nft/GetOLANDPrice?count=${count}&couponCode=${couponCode}`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
    };

    return axios(config)
      .then((response) => {
        if (response.data.isError) {
          return { error: true, data: response.data };
        }
        return { error: false, data: response.data };
      })
      .catch((err) => {
        return { error: true, data: err };
      });
  }

  async purchaseOLAND(
    data = {
      olandId: "",
      avatarId: "",
      avatarUsername: "",
      tiles: "",
      walletAddress: "",
      cargoSaleId: "",
    }
  ) {
    data = JSON.stringify(data);

    const config = {
      method: "get",
      url: `https://api.oasisplatform.world/api/Nft/purchaseOLAND`,
      headers: {
        Authorization: `Bearer ${this.token.jwtToken}`,
      },
      data,
    };

    return axios(config)
      .then((response) => {
        if (response.data.isError) {
          return { error: true, data: response.data };
        }
        return { error: false, data: response.data };
      })
      .catch((err) => {
        return { error: true, data: err };
      });
  }
}

export default NFT;

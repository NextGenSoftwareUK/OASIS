import axios from "axios"
import Avatar from "../avatar/avatar";

class SEEDS {
	constructor(){
		this.avatar = new Avatar()
		this.url = 'https://api.oasisplatform.world'
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

	async payWithSeedsUsingTelos(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/PayWithSeedsUsingTelosAccount`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async payWithSeedsUsingAvatar(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/PayWithSeedsUsingAvatar`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async rewardWithSeedsUsingTelos(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/RewardWithSeedsUsingTelosAccount`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async rewardWithSeedsUsingAvatar(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/RewardWithSeedsUsingAvatar`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async donateWithSeedsUsingTelos(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/DonateWithSeedsUsingTelosAccount`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async donateWithSeedsUsingAvatar(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/DonateWithSeedsUsingAvatar`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async sendInviteSeedsUsingTelos(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/SendInviteToJoinSeedsUsingTelosAccount`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async sendInviteSeedsUsingAvatar(data){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "post",
	      url: `${this.url}/api/seeds/SendInviteToJoinSeedsUsingAvatar`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}

	async getOrganisations(){
		this.token = await this.avatar.callLogin()

		if (this.token === -1) {
     		 return { error: true, data: { message: "You are not logged in!" } };
    	}

    	const config = {
	      method: "get",
	      url: `${this.url}/api/seeds/GetAllOrganisations`,
	      headers: {
	        Authorization: `Bearer ${this.token.jwtToken}`,
	      },
	      data,
	    };

	    return this._returnState(config)
	}
}
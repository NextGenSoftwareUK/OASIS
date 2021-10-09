import React from "react"
import Loader from "react-loader-spinner";
import {Link} from "react-router-dom"


import ReactGrid from "../../ReactGrid";
import {login} from "../../../functions"


import "../../../assets/scss/avatarWallet.scss"
import "../../../assets/scss/popup.scss"

class AvatarWallet extends React.Component {
	constructor(props){
		super(props)
		this.state = {
			columns: [
				{name: 'date', title: 'Date'},
				{name: 'note', title: 'Note'},
				{name: 'balance', title: 'Balance'},
				{name: 'provider', title: 'Provider'},
				{name: 'type', title: 'Type'}
			],
			columnWidth: [
				{columnName: 'date', width: 180},
				{columnName: 'note', width: 180},
				{columnName: 'balance', width: 180},
				{columnName: 'provider', width: 180},
				{columnName: 'type', width: 180}
			],
			rows: [],
			loading: true,
			loggedIn: true,
		}
	}

	async componentDidMount(){
		// Api calls
		var token, refresh, credentials;

        //If user object exists in localstorage, get the refresh token
        //and the jwtToken
        if (localStorage.getItem("user")) {
            credentials = JSON.parse(localStorage.getItem("credentials"));
            let avatar = await login(credentials);
            if (avatar !== -1) {
                token = avatar.jwtToken;
                refresh = avatar.refreshToken;
            }
            this.setState({loading: false})
        }

        //else (for now) show an alert and redirect to home
        else {
            // alert("not logged in");
            this.setState({ loggedIn: false });
            this.setState({loading: false})
        }
	}

	render() {
		return (
			<div className="popup">
				<div className="avatarWallet">
					<Link to="/" className="popup-cancel">
                        <span className="form-cross-icon">
                            <i className="fa fa-times"></i>
                        </span>
                    </Link>
                    <h2 className="avatarWallet-heading">Avatar Wallet</h2>
                    {this.state.loggedIn ? (
                    	<>		                	
		                    {this.state.loading ? (
		                        <Loader type="Oval" height={30} width={30} color="#fff" />
		              	        ) :
		                    	<>
		                        	<div className="avatarWallet-grid">
			                    		<ReactGrid 
			                    			rows={this.state.rows} 
			                    			columns={this.state.columns} 
			                    			columnWidths={this.state.columnWidth}
			                    		/>
		                    		</div>
		                    		
		                    	</>
		                    }
		                    
		                 </>
	                    ) : <h3>You are not logged in!</h3>
                	}
                </div>
			</div>
		)
	}
}

export default AvatarWallet

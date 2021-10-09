import React from "react"
import {Link} from "react-router-dom"


import Messages from "../../common/message/Messages"
import "../../../assets/scss/popup.scss"
import "../../../assets/scss/Message.scss"

class Message extends React.Component {
	constructor(props) {
		super(props)
	}

	render() {
		return (
			<div className="popup">
				<Link to="/" className="popup-cancel">
                    <span className="form-cross-icon">
                        <i className="fa fa-times"></i>
                    </span>
                </Link>
				<div className="message">
					<div className="message-left">
						<div class="card-body contacts_body">
						<ui class="contacts">
						<li class="active">
							<div class="d-flex bd-highlight">
								<div class="img_cont">
									<img src="https://static.turbosquid.com/Preview/001292/481/WV/_D.jpg" class="rounded-circle user_img"/>
									<span class="online_icon"></span>
								</div>
								<div class="user_info">
									<span>Khalid</span>
									<p>Kalid is online</p>
								</div>
							</div>
						</li>
						<li>
							<div class="d-flex bd-highlight">
								<div class="img_cont">
									<img src="https://2.bp.blogspot.com/-8ytYF7cfPkQ/WkPe1-rtrcI/AAAAAAAAGqU/FGfTDVgkcIwmOTtjLka51vineFBExJuSACLcBGAs/s320/31.jpg" class="rounded-circle user_img"/>
									<span class="online_icon offline"></span>
								</div>
								<div class="user_info">
									<span>Taherah Big</span>
									<p>Taherah left 7 mins ago</p>
								</div>
							</div>
						</li>
						<li>
							<div class="d-flex bd-highlight">
								<div class="img_cont">
									<img src="https://i.pinimg.com/originals/ac/b9/90/acb990190ca1ddbb9b20db303375bb58.jpg" class="rounded-circle user_img"/>
									<span class="online_icon"></span>
								</div>
								<div class="user_info">
									<span>Sami Rafi</span>
									<p>Sami is online</p>
								</div>
							</div>
						</li>
						<li>
							<div class="d-flex bd-highlight">
								<div class="img_cont">
									<img src="http://profilepicturesdp.com/wp-content/uploads/2018/07/sweet-girl-profile-pictures-9.jpg" class="rounded-circle user_img"/>
									<span class="online_icon offline"></span>
								</div>
								<div class="user_info">
									<span>Nargis Hawa</span>
									<p>Nargis left 30 mins ago</p>
								</div>
							</div>
						</li>
						<li>
							<div class="d-flex bd-highlight">
								<div class="img_cont">
									<img src="https://static.turbosquid.com/Preview/001214/650/2V/boy-cartoon-3D-model_D.jpg" class="rounded-circle user_img"/>
									<span class="online_icon offline"></span>
								</div>
								<div class="user_info">
									<span>Rashid Samim</span>
									<p>Rashid left 50 mins ago</p>
								</div>
							</div>
						</li>
						</ui>
					</div>
					</div>
					<div className="message-right">
						<Messages />
					</div>
				</div>
			</div>
		)
	}
}

export default Message
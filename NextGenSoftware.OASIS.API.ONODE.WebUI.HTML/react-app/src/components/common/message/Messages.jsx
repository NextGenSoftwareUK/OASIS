import React from "react"

class Messages extends React.Component {
	constructor(props) {
		super(props)
	}

	render(){
		return (
			<div class="mesgs">
                <div class="msg_history">
                    {this.props.messageList.map((message)=>(<>
                        {message.from === 'me' ? (
                            <div class="outgoing_msg">
                                <div class="sent_msg">
                                    <p>
                                        {message.message}
                                    </p>
                                    <span class="time_date">
                                        {" "}
                                        {message.date}
                                    </span>{" "}
                                </div>
                            </div>
                            ) : (
                            <div class="incoming_msg">
                                <div class="incoming_msg_img">
                                    {" "}
                                    <img
                                        src={message.img}
                                        alt={message.name}
                                    />{" "}
                                </div>
                                <div class="received_msg">
                                    <div class="received_withd_msg">
                                        <p>
                                            {message.message}
                                        </p>
                                        <span class="time_date">
                                            {" "}
                                            {message.date}
                                        </span>
                                    </div>
                                </div>
                            </div>
                            )}</>
                    ))}
                    
                    
                    
                </div>
                <div class="type_msg">
                    <div class="input_msg_write">
                        <input
                            type="text"
                            class="write_msg"
                            placeholder="Type a message"
                        />
                        <button
                            class="msg_send_btn"
                            type="button"
                        >
                            <i
                                class="fa fa-paper-plane-o"
                                aria-hidden="true"
                            ></i>
                        </button>
                    </div>
                </div>
            </div>
		)
	}
}

export default Messages
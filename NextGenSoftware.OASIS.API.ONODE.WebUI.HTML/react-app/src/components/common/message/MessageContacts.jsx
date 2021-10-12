import React from "react"

class MessageContacts extends React.Component {
	constructor(props) {
		super(props)

	}

	render() {
		return (
			<div class="inbox_people">
                                    <div class="headind_srch">
                                        <div class="recent_heading">
                                            <h4>Recent</h4>
                                        </div>
                                        <div class="srch_bar">
                                            <div class="stylish-input-group">
                                                <input
                                                    type="text"
                                                    class="search-bar"
                                                    placeholder="Search"
                                                />
                                                <span class="input-group-addon">
                                                    <button type="button">
                                                        {" "}
                                                        <i
                                                            class="fa fa-search"
                                                            aria-hidden="true"
                                                        ></i>{" "}
                                                    </button>
                                                </span>{" "}
                                            </div>
                                        </div>
                                    </div>
                                    <div class="inbox_chat">
                                        {this.props.contactList.map((contact)=>(
                                            <div class={contact.active ? "chat_list active_chat" : "chat_list"}>
                                            <div class="chat_people">
                                                <div class="chat_img">
                                                    {" "}
                                                    <img
                                                        src={contact.img}
                                                        alt={contact.name}
                                                    />
                                                </div>
                                                <div class="chat_ib">
                                                    <h5>
                                                        {contact.name}{" "}
                                                        <span class="chat_date">
                                                            {contact.date}
                                                        </span>
                                                    </h5>
                                                    <p>
                                                        {contact.lastMessage}
                                                    </p>
                                                </div>
                                            </div>
                                        </div>
                                        ))}        
                                    </div>
                                </div>	
		)
	}
}

export default MessageContacts

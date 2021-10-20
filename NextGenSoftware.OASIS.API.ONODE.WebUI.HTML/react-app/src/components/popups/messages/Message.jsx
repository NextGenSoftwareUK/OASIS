import React from "react";
import { Link } from "react-router-dom";
import { Modal } from "react-bootstrap";

import MessageContacts from "../../common/message/MessageContacts";
import Messages from "../../common/message/Messages";
import "../../../assets/scss/popup.scss";
import "../../../assets/scss/message.scss";
import avatar from "../../../assets/images/loggedin.png"

class Message extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            contactList: [
                {
                    name: 'Test 123',
                    img: avatar,
                    date: '13th October 2021',
                    lastMessage: 'Test 123',
                    active: true
                },
                {
                    name: 'Test 123',
                    img: avatar,
                    date: '13th October 2021',
                    lastMessage: 'Test 123',
                    active: false
                }
            ],
            messageList: [
                {
                    from: 'Admin',
                    img: avatar,
                    date: '10th October',
                    message: 'Hey what are you doing?'
                },
                {
                    from: 'me',
                    date: '10th October',
                    message: 'Making chating work on React'
                },
                {
                    from: 'Admin',
                    img: avatar,
                    date: 'Yesterday',
                    message: 'OK Good luck'
                },
                {
                    from: 'me',
                    date: '8:00 am',
                    message: 'Thank you'
                },
            ]
        }
    }

    render() {
        return (
            <Modal
                size="xl"
                show={true}
                dialogClassName="modal-90w"
                onHide={() => this.props.history.push("/")}
            >
                <Modal.Header closeButton>
                    <Modal.Title>Messages</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <div class="container">
                        <div class="messaging">
                            <div class="inbox_msg">
                                <MessageContacts 
                                    contactList={this.state.contactList}
                                />
                                <Messages messageList={this.state.messageList}/>
                            </div>
                        </div>
                    </div>
                </Modal.Body>
            </Modal>
        );
    }
}

export default Message;

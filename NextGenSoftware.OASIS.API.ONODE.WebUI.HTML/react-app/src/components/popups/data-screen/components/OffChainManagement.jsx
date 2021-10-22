import React from "react";

import { Modal } from "react-bootstrap";

import { ToastContainer, toast } from "react-toastify";

class OffChainManagement extends React.Component {
  state = {
    tagsList: [
      { id: 1, name: "Item 01", status: false },
      { id: 2, name: "Item 02", status: false },
      { id: 3, name: "Item 03", status: false },
      { id: 4, name: "Item 04", status: false },
      { id: 5, name: "Item 05", status: false },
      { id: 6, name: "Item 06", status: false },
      { id: 7, name: "Item 07", status: false },
      { id: 8, name: "Item 08", status: false },
    ],
  };

  handleInputTagChange = (item) => {
    let tags = [...this.state.tagsList];

    const index = tags.indexOf(item);
    tags[index].status = !tags[index].status;

    this.setState({
      tagsList: tags,
    });
  };

  toggleAllTags = (action) => {
    let tags = [...this.state.tagsList];
    tags.map((tag) => {
      tag.status = action == "add" ? true : false;
    });

    this.setState({
      tagsList: tags,
    });
  };

  toggleSelectedTags = (action) => {
    let tags = [...this.state.tagsList];

    tags.map((tag) => {
      tag.status = action == "add" ? true : false;
    });
  };

  saveData = () => {
    toast.success(" Your Data is Saved!");
  };
  render() {
    const { show, hide } = this.props;

    return (
      <>
        <ToastContainer
          position="top-center"
          autoClose={5000}
          hideProgressBar={false}
          newestOnTop={false}
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
        />
        <Modal
          centered
          className="custom-modal custom-popup-component"
          show={show}
          onHide={() => hide("data", "offChainManagement")}
        >
          <Modal.Body>
            <span
              className="form-cross-icon"
              onClick={() => hide("data", "offChainManagement")}
            >
              <i className="fa fa-times"></i>
            </span>

            <div className="popup-container default-popup">
              <div className="data-screen-container off-chain-management">
                <h2>Off Chain Management</h2>

                <h3>On Chain Provider</h3>
                <div className="off-chain-container">
                  <ul className="list-item list-box">
                    {this.state.tagsList.map((tag) =>
                      tag.status ? null : (
                        <li key={tag.id}>
                          <label>
                            <input
                              type="checkbox"
                              name="checkbox"
                              checked={tag.status}
                              onChange={() => this.handleInputTagChange(tag)}
                            />
                            <span>{tag.name}</span>
                          </label>
                        </li>
                      )
                    )}
                  </ul>

                  <div className="buttons-list">
                    <button>ADD</button>

                    <button onClick={() => this.toggleAllTags("add")}>
                      ADD ALL
                    </button>

                    <button>REMOVE</button>

                    <button onClick={() => this.toggleAllTags("remove")}>
                      REMOVE ALL
                    </button>
                  </div>

                  <ul className="list-item list-box">
                    {this.state.tagsList.map((tag) =>
                      tag.status ? (
                        <li key={tag.id}>
                          <label>
                            <input
                              type="checkbox"
                              name="checkbox"
                              checked={tag.status}
                              onChange={() => this.handleInputTagChange(tag)}
                            />
                            <span>{tag.name}</span>
                          </label>
                        </li>
                      ) : null
                    )}
                  </ul>
                </div>

                <div className="save-button-container">
                  <button onClick={this.saveData} type="submit">
                    Save
                  </button>
                </div>
              </div>
            </div>
          </Modal.Body>
        </Modal>
      </>
    );
  }
}

export default OffChainManagement;

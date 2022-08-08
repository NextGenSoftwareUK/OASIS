import React from "react";

import { Modal } from "react-bootstrap";

import { toast } from "react-toastify";

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
      tag.status = action === "add" ? true : false;
    });

    this.setState({
      tagsList: tags,
    });
  };

  toggleSelectedTags = (action) => {
    let tags = [...this.state.tagsList];

    tags.map((tag) => {
      tag.status = action === "add" ? true : false;
    });
  };

  saveData = () => {
    toast.success(" Your Data is Saved!");
  };
  render() {
    const { show, hide } = this.props;

    return (
      <>
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
                <h1 className="single-heading">Off Chain Management</h1>

                <form className="custom-form" style={{padding: 0}}>
                  <div className="off-chain-container">
                    <div className="All-provider">
                      <h3>All Provider</h3>
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
                    </div>

                    <div className="buttons-list">
                      {/* <button>ADD</button> */}

                      {/* <button className=" sm-button" onClick={() => this.toggleAllTags("add")}>
                        ADD ALL
                      </button> */}

                      {/* <button>REMOVE</button> */}

                      {/* <button className=" sm-button" onClick={() => this.toggleAllTags("remove")}>
                        REMOVE ALL
                      </button> */}
                    </div>
                    <div className="off-chain-provider">
                      <h3>Off Chain Provider</h3>
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
                  </div>

                  <button className="submit-button" onClick={this.saveData} type="submit">
                      Save
                  </button>
                </form>
              </div>
            </div>
          </Modal.Body>
        </Modal>
      </>
    );
  }
}

export default OffChainManagement;

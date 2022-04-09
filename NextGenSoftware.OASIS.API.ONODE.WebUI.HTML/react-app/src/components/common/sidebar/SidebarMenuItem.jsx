import React, { Component } from "react";

class SidebarMenuItem extends React.Component {
  state = {
    show: false,
  };

  expandHandler = () => {
    this.setState({
      show: !this.state.show,
    });
  };

  render() {
    const { item } = this.props;
    console.log(item);
    return (
      <>
        <li>
          <a onClick={this.expandHandler}>{item.name}</a>

          <ul
            className={`sidebar-inner-menu ${this.state.show ? "show" : ""}`}
            id={item.id}
          >
            {item.subMenu.map((subItem, index) => (
              <li key={index}>
                {/* <a className={subItem.disabled ? 'disbale' : ''}>{subItem.name}</a> */}
                {subItem.disabled ? (
                  <a className="disabled">{subItem.name}</a>
                ) : subItem.path ? (
                  <a onClick={() => window.open(subItem.path, "_blank")}>
                    {subItem.name}
                  </a>
                ) : (
                  <a
                    onClick={() =>
                      this.props.toggleScreenPopup(item.name, subItem.popupName)
                    }
                  >
                    {subItem.name}
                  </a>
                )}
              </li>
            ))}
          </ul>
        </li>
      </>
    );
  }
}

export default SidebarMenuItem;

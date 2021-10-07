import React, { Component } from 'react';
import data from './SidebarData';
import SidebarMenuItem from './SidebarMenuItem';

class Sidebar extends React.Component {

    toggleSidebarMenuItem = () => {

    }

    render() {
        console.log(data)
        return (
            <div className={`sidebar ${this.props.showSidebar ? "sidebar-show" : ""}`}>
                <ul className="sidebar-list">
                    {
                        data.map((item, index) =>
                            <SidebarMenuItem
                                key={index}
                                item={item}
                                hideSideNav={this.props.toggleSidebar}
                                toggleDataScreenPopup={this.props.toggleDataScreenPopup}
                                toggleNftPopup={this.props.toggleNftPopup} 
                            />
                        )
                    }
                </ul>
            </div>
        );
    }
}

export default Sidebar;
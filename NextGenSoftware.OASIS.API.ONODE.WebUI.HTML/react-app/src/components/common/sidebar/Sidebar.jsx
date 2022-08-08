import React from 'react';
import data from './SidebarData';
import SidebarMenuItem from './SidebarMenuItem';

class Sidebar extends React.Component {

    render() {
        return (
            <div className={`sidebar ${this.props.showSidebar ? "sidebar-show" : ""}`}>
                <ul className="sidebar-list">
                    {
                        data.map((item, index) =>
                            <SidebarMenuItem
                                key={index}
                                item={item}
                                hideSideNav={this.props.toggleSidebar}
                                toggleScreenPopup={this.props.toggleScreenPopup}
                            />
                        )
                    }
                </ul>
            </div>
        );
    }
}

export default Sidebar;
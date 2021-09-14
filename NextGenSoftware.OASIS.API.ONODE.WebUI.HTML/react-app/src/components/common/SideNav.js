import React from 'react';
import data from '../SideNavData'
import SideMenuItems from './SideMenuItems';

const SideNav = (props) => {
    return (
        <div className={`side-nav ${props.showSidebar ? "side-nav-show" : ""}`}>
            <ul className="side-nav-list">
                {data.map((menu, index) => <SideMenuItems menu={menu} key={index} hideSideNav={props.toggleSidebar} />)}
            </ul>
        </div>
    );
}
 
export default SideNav;

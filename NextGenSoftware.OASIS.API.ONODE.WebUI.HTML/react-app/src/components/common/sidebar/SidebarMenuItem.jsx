import React, { Component } from 'react';

class SidebarMenuItem extends React.Component {

    state = {
        show: false
    }

    expandHandler = () => {
        this.setState({
            show: !this.state.show
        })
    }

    render() {
        const { item } = this.props;
        console.log(item)
        return (
            <>
                <li>
                    <a onClick={this.expandHandler}>{item.name}</a>

                    <ul className={`sidebar-inner-menu ${this.state.show ? 'show' : ''}`} id={item.id}>
                        {
                            item.subMenu.map((subItem, index) =>
                                <li key={index}>
                                    {
                                        subItem.disabled 
                                        ? 
                                            <a className='disabled'>{subItem.name}</a>
                                        :
                                            <a 
                                                target={subItem.externalLink ? '_blank': ''}
                                                href={subItem.path} 
                                                onClick={
                                                    () => this.props.toggleScreenPopup(item.name, subItem.popupName)
                                                }
                                            >{subItem.name}</a>
                                    }
                                </li>
                            )
                        }
                    </ul>
                </li>
            </>
        );
    }
}

export default SidebarMenuItem;
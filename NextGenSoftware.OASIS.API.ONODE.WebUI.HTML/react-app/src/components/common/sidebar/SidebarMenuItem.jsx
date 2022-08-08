import React from 'react';
import oasisApi from "oasis-api"

class SidebarMenuItem extends React.Component {

    async componentDidMount(){
      const auth = new oasisApi.Auth()
      const user = await auth.getUser()
      if(!user.error){
        this.setState({user: user.data})
        console.log(user);
      }
    }

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
        return (
            <>
                <li>
                    <a onClick={this.expandHandler}>{item.name}</a>

                    <ul className={`sidebar-inner-menu ${this.state.show ? 'show' : ''}`} id={item.id}>
                        {
                            item.subMenu.map((subItem, index) =>
                                <li key={index}>
                                    {/* {
                                        subItem.disabled || (subItem.loginRequired && !this.state.user)
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
                                    } */}

                                        <a
                                            target={subItem.externalLink ? '_blank': ''}
                                            href={subItem.path}
                                            onClick={
                                                () => this.props.toggleScreenPopup(item.name, subItem.popupName)
                                            }
                                        >{subItem.name}</a>
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

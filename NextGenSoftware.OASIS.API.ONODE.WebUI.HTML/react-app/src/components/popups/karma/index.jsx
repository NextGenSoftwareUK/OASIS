import React  from 'react';

import ViewKarma from './components/ViewKarma';
import "../../../assets/scss/data-screen.scss";
class Karma extends React.Component {
    render() { 
        const props=this.props;
        return(
            <>
                <ViewKarma
                    show={props.karma.viewKarma}
                    hide={props.toggleScreenPopup}
                />
            </>
        );
    }
}
 
export default Karma;
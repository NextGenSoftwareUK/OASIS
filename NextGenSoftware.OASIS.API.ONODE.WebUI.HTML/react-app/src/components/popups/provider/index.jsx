import React from 'react';
import ActivityPub from './components/ActivityPub';
import CompareProviderSpeeds from './components/CompareProviderSpeeds';
import Eosio from './components/Eosio';
import Ethereum from './components/Ethereum';
import Holochain from './components/Holochain';
import Ipfs from './components/Ipfs';
import ManageAutoFailOver from './components/ManageAutoFailOver';
import ManageAutoReplicaton from './components/ManageAutoReplicaton';
import ManageLoadBalancing from './components/ManageLoadBalancing';
import ManageProviders from './components/ManageProviders';
import MongoDb from './components/MongoDb';
import Neo4j from './components/Neo4j';
import SearchProviders from './components/SearchProviders';
import Seeds from './components/Seeds';
import Solid from './components/Solid';
import SqlLite from './components/SqlLite';
import ThreeFold from './components/ThreeFold';
import ViewProviders from './components/ViewProviders';
import ViewProviderStats from './components/ViewProviderStats';

class Provider extends React.Component {

    render() {
        const props=this.props;
        return(
            <>
                <ActivityPub
                    show={props.provider.activityPub}
                    hide={props.toggleScreenPopup}
                />
                <CompareProviderSpeeds 
                    show={props.provider.compareProviderSpeeds}
                    hide={props.toggleScreenPopup}
                />
                <Eosio
                    show={props.provider.eosio}
                    hide={props.toggleScreenPopup}
                />
                <Ethereum
                    show={props.provider.ethereum}
                    hide={props.toggleScreenPopup}
                />
                <Holochain
                    show={props.provider.holochain}
                    hide={props.toggleScreenPopup}
                />
                <Ipfs
                    show={props.provider.ipfs}
                    hide={props.toggleScreenPopup}
                />
                <ManageAutoFailOver
                    show={props.provider.manageAutoFailOver}
                    hide={props.toggleScreenPopup}
                />
                <ManageAutoReplicaton
                    show={props.provider.manageAutoReplicaton}
                    hide={props.toggleScreenPopup}
                />
                <ManageLoadBalancing
                    show={props.provider.manageLoadBalancing}
                    hide={props.toggleScreenPopup}
                />
                <ManageProviders
                    show={props.provider.manageProviders}
                    hide={props.toggleScreenPopup}
                />
                <MongoDb
                    show={props.provider.mongoDb}
                    hide={props.toggleScreenPopup}
                />
                <Neo4j
                    show={props.provider.neo4j}
                    hide={props.toggleScreenPopup}
                />
                <SearchProviders
                    show={props.provider.searchProviders}
                    hide={props.toggleScreenPopup}
                />
                <Seeds
                    show={props.provider.seeds}
                    hide={props.toggleScreenPopup}
                />
                <Solid
                    show={props.provider.solid}
                    hide={props.toggleScreenPopup}
                />
                <SqlLite
                    show={props.provider.sqlLite}
                    hide={props.toggleScreenPopup}
                />
                <ThreeFold
                    show={props.provider.threeFold}
                    hide={props.toggleScreenPopup}
                />
                <ViewProviders
                    show={props.provider.viewProviders}
                    hide={props.toggleScreenPopup}
                />
                <ViewProviderStats
                    show={props.provider.viewProviderStats}
                    hide={props.toggleScreenPopup}
                />
                
            </>
        )
    }
}

export default Provider;
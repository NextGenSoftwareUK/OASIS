import { ApolloClient } from 'apollo-client';
import { InMemoryCache } from 'apollo-cache-inmemory';
import { HttpLink } from 'apollo-link-http';
import gql from "graphql-tag";

import { ApolloProvider } from '@apollo/react-hooks';
import React from 'react';
import ReactDOM from 'react-dom';
import Pages from './pages';

const cache = new InMemoryCache();
const link = new HttpLink({
  uri: 'http://localhost:4000/'
})

const client = new ApolloClient({
  cache,
  link
})

/*
ReactDOM.render(
  <ApolloProvider client={client}>
    <Pages />
  </ApolloProvider>, document.getElementById('root')
);
*/

var resultOutput;

const readData = ({ data }) => 
{
  console.log("data = " + data);
  const { allData = [] } = data;
  console.log("allData = " + allData);

  allData.forEach((item) => 
  {
    console.log ("item = " + item);
  });
}

// ... above is the instantiation of the client object.
client
  .query({
    query: gql`
      query GetLaunch {
        launch(id: 56) {
          id
          mission {
            name
          }
        }
      }
    `
  })
//.then(result => console.log(result))
.then(function(result)
  {
    console.log("hello?");
    resultOutput = result;
    console.log("resultOutput2 = " + resultOutput);
  });



  //.then(readData);

//   .then(result => alert("result = " + result), // shows "done!" after 1 second
//   error => alert("error = " + error) // doesn't run
// );
  
  //.then(result => resultOutput = result);
  //  .then(result => console.log(result));
  
  console.log("resultOutput = " + resultOutput);
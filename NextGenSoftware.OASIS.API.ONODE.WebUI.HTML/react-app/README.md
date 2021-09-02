# React App for the OASIS

This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).

## Project Structure
```
├── react-app 
│   ├── public
│   │   └── index.html
│   ├── src
│   │   ├── CSS
│   │   ├── Components
│   │   ├── img
│   │   └── index.js
```

All of the files are stored in react-app folder.

- `index.html` is the main html file
- `CSS` folder contains css files for each component
- `Components` folder contains different components used in the app
- `index.js` is the main javaScript file needed for the react-app

## CSS Syntax rules
Please follow these rules so that the other CSS classes does not gets messed up.

For example if you create a component called `Login.js` It's outer most tag element should have a class of `login` and the child elements of login should have a class name started with `login-`

The CSS file should be named `Login.css` and should be stored under the `CSS` folder.

## How to run the app

First make sure you are in the react-app folder. Then run this command

```
npm i
```
This will install all the third-part components used in the project

After this run the following command to open the project in dev mode
```
npm start
```
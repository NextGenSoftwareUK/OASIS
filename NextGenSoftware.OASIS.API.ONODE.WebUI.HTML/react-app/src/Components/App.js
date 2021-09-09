import "../CSS/App.css";
import Navbar from "./Navbar";
import Karma from "../pages/Karma";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";

function App() {
    return (
        <Router>
            <header className="App-header">
                <Navbar />
            </header>
            <Switch>
                {/* Todo: Here we will write the components for the main page */}
                <Route exact path="/karma" component={Karma} />
            </Switch>
        </Router>
    );
}

export default App;

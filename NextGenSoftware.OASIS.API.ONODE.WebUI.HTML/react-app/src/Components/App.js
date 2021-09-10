import '../CSS/App.css'
import Navbar from './Navbar'
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import PayWithSeeds from './seeds/PayWithSeeds'
import SendInvite from './seeds/SendInvite'
import Karma from "../pages/Karma"
import Home from "../pages/Home"
import '../CSS/Seeds.css'

function App() {
  return (
    <Router>
      <header className="App-header">
        <Navbar />
      </header>
      <Switch>
        <Route path="/payWithSeeds">
          <PayWithSeeds />
        </Route>
        <Route path="/">
          <Home/>
        </Route>
        <Route path="/sendInvite">
          <SendInvite />
        </Route>
        <Route path="/karma">
          <Karma/>
        </Route>
      </Switch>
    </Router>
  )
}

export default App;

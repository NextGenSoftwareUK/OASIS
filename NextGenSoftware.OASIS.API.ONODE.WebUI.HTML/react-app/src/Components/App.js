import Navbar from './NavBar/Navbar'
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import PayWithSeeds from './seeds/PayWithSeeds'
import SendInvite from './seeds/SendInvite'

// ============= All of the css imports ===========
import '../CSS/App.css'
import '../CSS/Navbar.css'
import '../CSS/Login.css'
import '../CSS/SideNav.css'
import '../CSS/Alert.css'
import '../CSS/Seeds.css'
// ================================================

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
        <Route path="/donateWithSeeds">
          <PayWithSeeds seedType="Donate" />
        </Route>
        <Route path="/rewardWithSeeds">
          <PayWithSeeds seedType="Reward" />
        </Route>
        <Route path="/sendInvite">
          <SendInvite />
        </Route>
      </Switch>
    </Router>
  )
}

export default App
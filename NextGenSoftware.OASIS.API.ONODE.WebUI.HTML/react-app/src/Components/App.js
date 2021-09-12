import '../CSS/App.css'
import Navbar from './Navbar'
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import PayWithSeeds from './seeds/PayWithSeeds'
import SendInvite from './seeds/SendInvite'
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
import '../CSS/App.css'
import Navbar from './Navbar'
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import Seeds from './PayWithSeeds'
import PayWithSeeds from './PayWithSeeds'

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
      </Switch>
    </Router>
  )
}

export default App
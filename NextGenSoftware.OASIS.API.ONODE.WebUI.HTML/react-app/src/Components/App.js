import '../CSS/App.css'
import Navbar from './Navbar'
import { BrowserRouter as Router, Switch } from 'react-router-dom'
function App() {
  return (
    <Router>
      <header className="App-header">
        <Navbar />
      </header>
      <Switch>
        {/* Todo: Here we will write the components for the main page */}
      </Switch>
    </Router>
  )
}

export default App
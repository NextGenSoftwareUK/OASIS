// This test file uses the tape testing framework.
// To learn more, go here: https://github.com/substack/tape
const { Config, Scenario } = require("@holochain/holochain-nodejs")
Scenario.setTape(require("tape"))

const dnaPath = "./dist/our_world.dna.json"
const agentAlice = Config.agent("alice")
const dna = Config.dna(dnaPath)
const instanceAlice = Config.instance(agentAlice, dna)
const scenario = new Scenario([instanceAlice])

/*
scenario.runTape("description of example test", async (t, { alice }) => {
  // Make a call to a Zome function
  // indicating the function, and passing it an input
  const addr = alice.call("my_zome", "create_my_entry", {"entry" : {"content":"sample content"}})
  const result = alice.call("my_zome", "get_my_entry", {"address": addr.Ok})

  // check for equality of the actual and expected results
  t.deepEqual(result, { Ok: { App: [ 'my_entry', '{"content":"sample content"}' ] } })
})
*/

scenario.runTape("test", async (t, { alice }) => {
  // Make a call to a Zome function
  // indicating the function, and passing it an input
  const result = alice.call("our_world_core", "test", {"message":"blah"})

  // check for equality of the actual and expected results
  t.deepEqual(result, { Ok: 'Hello, welcome to Our World!' })
})

scenario.runTape("test2", async (t, { alice }) => {
  // Make a call to a Zome function
  // indicating the function, and passing it an input
  const result = alice.call("our_world_core", "test2", {})

  // check for equality of the actual and expected results
  t.deepEqual(result, { Ok: 'Hello, welcome to Our World!' })
})
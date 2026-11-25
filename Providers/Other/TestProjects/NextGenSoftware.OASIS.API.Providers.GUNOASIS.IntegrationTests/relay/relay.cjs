// Disposable GUN relay for the native storage suite: loopback only, radisk persistence in the given directory.
const http = require("http");
const Gun = require("gun");
const [portText, directory] = process.argv.slice(2);
const port = Number(portText);
if (!Number.isInteger(port) || port < 1024 || port > 65535 || !directory) throw new Error("usage: relay.cjs <port> <radisk dir>");
const server = http.createServer(Gun.serve(__dirname)).listen(port, "127.0.0.1");
Gun({ web: server, file: directory, localStorage: false, axe: false, multicast: false });

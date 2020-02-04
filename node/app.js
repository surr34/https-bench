const express = require("express");
const fs = require("fs");
const https = require("https");
const http = require("http");
const cluster = require('cluster');
const numCPUs = require('os').cpus().length;

const credentials = {
    key: fs.readFileSync("./self.key.pem"),
    cert: fs.readFileSync("./self.cert.pem")
};

const handler = (req, res) => {
    const { method, httpVersion, protocol, hostname, path, url, query, params, headers, body } = req;
    res.send({ method, httpVersion, protocol, hostname, path, url, query, params, headers, body });
};

const app = express();
app.use(express.json());
app.get("*", handler);
app.post("*", handler);
app.put("*", handler);
app.delete("*", handler);

if (cluster.isMaster) {
    for (var i = 0; i < numCPUs; i++) {
        cluster.fork();
    }
} else {
    https.createServer(credentials, app).listen(21112);
    http.createServer(app).listen(11112);
}

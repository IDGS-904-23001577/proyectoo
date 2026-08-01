const PROXY_CONFIG = [
  {
    context: [
      "/api",
      "/hubs"
    ],
    target: "http://localhost:5082",
    secure: false,
    changeOrigin: true,
    ws: true
  }
]

module.exports = PROXY_CONFIG;

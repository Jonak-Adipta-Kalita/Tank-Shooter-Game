#!/bin/bash
set -e

# Substitute the $PORT env variable into the nginx config
envsubst '${PORT}' < ./nginx.conf.template > /etc/nginx/nginx.conf

# Start nginx in the background (handles health checks + WebSocket proxy)
nginx -g "daemon off;" &

# Start the Godot game server in the foreground on fixed port 7777
./game.x86_64 --headless --dedicated-server

#!/bin/bash
set -e

echo "Starting Redis Cluster..."

/usr/local/bin/create-cluster start

ports=(30001 30002 30003)
nodes=()

# Wait for the Redis processes before assigning slots.
for port in "${ports[@]}"; do
  until redis-cli -p "$port" ping 2>/dev/null | grep -qx 'PONG'; do
    sleep 0.1
  done
  nodes+=("127.0.0.1:$port")
done

echo "Creating Redis Cluster..."
redis-cli --cluster create "${nodes[@]}" --cluster-replicas 0 --cluster-yes

# Every node must see all slots as ready before clients connect.
for port in "${ports[@]}"; do
  until redis-cli -p "$port" cluster info | grep -q '^cluster_state:ok'; do
    sleep 0.1
  done
done

echo "Redis Cluster is ready."
tail -f /dev/null

#!/bin/bash
set -e

echo "Starting Redis Cluster..."

/usr/local/bin/create-cluster start

echo "Creating Redis Cluster..."
yes yes | /usr/local/bin/create-cluster create

echo "Cluster status:"
redis-cli -p 30001 cluster info
echo "Cluster nodes:"
redis-cli -p 30001 cluster nodes
echo "Cluster slots:"
redis-cli -p 30001 cluster slots

until redis-cli -p 30001 cluster info | grep -q '^cluster_state:ok'; do
  echo "Redis Cluster is not ready yet:"
  redis-cli -p 30001 cluster info || true
  sleep 1
done

echo "Redis Cluster is ready."
tail -f /dev/null

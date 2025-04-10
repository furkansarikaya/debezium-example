#!/bin/bash

# Create topic first
echo "Creating transfer-events topic..."
docker exec -it kafka kafka-topics --create --topic transfer-events --bootstrap-server kafka:29092 --partitions 1 --replication-factor 1 --if-not-exists

# Wait for Kafka to be ready
echo "Waiting for Debezium Connect to be ready..."
sleep 10

# Register Postgres connector using the JSON file
echo "Registering Postgres connector from JSON file..."
curl -i -X POST -H "Accept:application/json" -H "Content-Type:application/json" localhost:8083/connectors/ -d @debezium-connector.json

# Display connectors status
echo -e "\nConnector status:"
curl -s localhost:8083/connectors/transfer-connector/status | jq

echo -e "\nAll available connectors:"
curl -s localhost:8083/connectors | jq 
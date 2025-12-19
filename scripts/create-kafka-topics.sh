#!/bin/bash

docker compose exec kafka /opt/kafka/bin/kafka-topics.sh \
  --create \
  --topic account-events \
  --bootstrap-server localhost:9092 \
  --partitions 6 \
  --replication-factor 1 \
  --if-not-exists

docker compose exec kafka /opt/kafka/bin/kafka-topics.sh \
  --create \
  --topic transaction-events \
  --bootstrap-server localhost:9092 \
  --partitions 6 \
  --replication-factor 1 \
  --if-not-exists

echo "topics created:"
docker compose exec kafka /opt/kafka/bin/kafka-topics.sh \
  --list \
  --bootstrap-server localhost:9092

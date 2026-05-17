FROM confluentinc/cp-kafka-connect:7.6.0 AS elasticsearch-plugin

RUN confluent-hub install --no-prompt confluentinc/kafka-connect-elasticsearch:15.1.2

FROM debezium/connect:2.6

COPY --from=elasticsearch-plugin \
    /usr/share/confluent-hub-components/confluentinc-kafka-connect-elasticsearch \
    /kafka/connect/confluentinc-kafka-connect-elasticsearch

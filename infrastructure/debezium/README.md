# Debezium ve Elasticsearch Connector Notlari

Bu klasordeki `.json` dosyalari Kafka Connect REST API'ye dogrudan gonderilir.
JSON standardi yorum satiri desteklemedigi icin aciklamalar config dosyalarinin
icinde tutulmaz. Aksi halde `curl --data-binary @...` ile connector register
edilirken payload gecersiz olur.

## `connector-config.json`

PostgreSQL WAL kayitlarini okuyup Kafka topic'lerine yazan Debezium source
connector konfigudur.

| Alan | Amac |
|------|------|
| `connector.class` | PostgreSQL icin Debezium source connector sinifini secer. |
| `database.hostname` | Docker network icindeki PostgreSQL servis adidir. |
| `database.port` | Container icindeki PostgreSQL portudur; host portu olan `55432` degil, `5432` kullanilir. |
| `database.user` / `database.password` | Logical replication icin PostgreSQL kullanici bilgileridir. |
| `database.dbname` | CDC yapilacak veritabani adidir. |
| `topic.prefix` | Kafka topic adinin basina gelir. `db` oldugu icin topicler `db.booking.tickets` ve `db.payment.payments` olur. |
| `plugin.name` | PostgreSQL logical replication icin `pgoutput` plugin'ini kullanir. |
| `publication.name` | PostgreSQL tarafindaki `ticketgate_pub` publication'ini dinler. |
| `schema.include.list` | CDC kapsamindaki schemalari sinirlar: yalnizca `booking,payment`. |
| `table.include.list` | CDC kapsamindaki tablolari sinirlar: `booking.tickets,payment.payments`. |
| `decimal.handling.mode` | PostgreSQL decimal/numeric alanlarini JSON number olarak uretir; Elasticsearch float mapping'iyle uyumludur. |
| `transforms` | Debezium mesajini Elasticsearch/Kibana icin daha sade hale getiren SMT zinciridir. |
| `transforms.unwrap.type` | Debezium envelope yapisini kaldirip row'un son halini mesaj govdesine tasir. |
| `transforms.unwrap.drop.tombstones` | Delete tombstone mesajlarini dusurmez; sink tarafinda delete davranisi icin korunur. |
| `transforms.unwrap.add.fields` | Mesaja operasyon, tablo ve kaynak timestamp metadata alanlarini ekler. |
| `transforms.unwrap.add.fields.prefix` | Metadata alanlarina prefix eklemez; alan adlari daha okunabilir kalir. |
| `transforms.renameMetadata.type` | Metadata alan adlarini yeniden adlandiran Kafka Connect SMT'dir. |
| `transforms.renameMetadata.renames` | `op` alanini `operation`, `source_ts_ms` alanini `sourceTimestampMs` yapar. |
| `key.converter` / `value.converter` | Kafka mesajlarini JSON olarak yazar. |
| `*.schemas.enable` | Schema Registry kullanmadan schemaless JSON uretir. |
| `heartbeat.interval.ms` | Connector'in canli oldugunu gosteren heartbeat mesaj araligidir. |
| `slot.name` | PostgreSQL logical replication slot adidir. |

## `elasticsearch-sink-config.json`

Kafka topic'lerinden okuyup Elasticsearch index'lerine yazan sink connector
konfigudur.

| Alan | Amac |
|------|------|
| `connector.class` | Confluent Elasticsearch sink connector sinifini secer. |
| `tasks.max` | Bu connector icin calisacak maksimum task sayisidir. Local ortamda `1` yeterlidir. |
| `topics` | Elasticsearch'e aktarilacak Kafka topic listesidir. |
| `connection.url` | Docker network icindeki Elasticsearch adresidir. |
| `type.name` | Eski Elasticsearch type alanidir; ES 8 uyumlulugu icin `_doc` kullanilir. |
| `key.ignore` | Kafka key'ini Elasticsearch document id olarak kullanmaz. |
| `schema.ignore` | Schemaless JSON kullandigimiz icin schema bilgisini beklemez. |
| `key.converter` / `value.converter` | Sink tarafinda da JSON converter kullanir. |
| `*.schemas.enable` | Kafka'daki schemaless JSON mesajlarini dogru okumak icin `false` olmalidir. |
| `behavior.on.null.values` | Null value/tombstone kayitlarinda Elasticsearch tarafinda delete davranisi uygular. |
| `flush.synchronously` | Topic adini degistiren `TimestampRouter` SMT icin gereklidir. |
| `transforms.addTimestamp.type` | Mesaja Kafka Connect isleme zamanini `@timestamp` olarak ekler. |
| `transforms.routeByMonth.type` | Topic adini Elasticsearch index adina donusturen TimestampRouter SMT'dir. |
| `transforms.routeByMonth.topic.format` | Index formatini `ticketgate-${topic}-${timestamp}` yapar. |
| `transforms.routeByMonth.timestamp.format` | Index tarih suffix'ini `yyyy.MM` formatinda uretir. |

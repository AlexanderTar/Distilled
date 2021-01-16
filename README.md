# Distilled - **dist**ributed log

This repository contains a primitive attempt to implement distributed log system. It is not by any means a production-ready system and is created purely for educational and experimental purposes.

There are two main components for Distilled: server and API. Server is a somewhat complex implementation of cluster node for multiple-follower replication. API implements RESTful endpoint for interaction with the system.

Distilled system follows leader-follower pattern. Etcd is used for consensus-related problems such as leader election as well as distributed storage for cluster metadata. No matter what the cluster size is, there is always one leader. All write requests are directed to it. Read requests are randomly distributed among all nodes. Google's grpc is used for all interservice communication.

Distilled provides very limited guarantees with regards to availability, however it is aiming to maintain a high level of consistency.

## Running Distilled

Distilled is using convinience of Docker containerisation for easy demonstation. In order to bring up a fully functioning cluster you need only one command:

```
docker-compose up --scale distilled-server=<NUM_OF_REPLICAS>
```

`NUM_OF REPLICAS` can be any number. if `--scale` argument is not specified, it runs one replica only, which makes the system trivial.

Distilled supports elastic resource management: if new nodes are added to the cluster (by, for example, running `docker-compose up` with a higher number of replicas in a separate console window) they will automatically catch up with existing log. If current leader dies, a new leader is immediately elected.

Once Distilled is up and leader has been elected, DistilledAPI is ready to serve user requests at `http://localhost:5000`. There is two methods implemented for testing purposes:

Reading stored message at specified offset:
```
GET http://localhost:5000/Data?offset=<offset>

response = {
    "message": <message_body>,
    "offset": <stored_offset>
}

```

Writing message to be stored:
```
POST http://localhost:5000/Data

request = {
    "message": <message_body>
}

response = {
    "offset": <stored_offset>
}
```

## TODO

The next step is to implement human-friendly error handling.

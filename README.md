# RecordReplay
Make any integration test fast!

## What is Record Replay?
This repo is a demonstration of a caching technique for slow dependencies of a system under test.
The idea is to create a RR cache of the dependency, which will spin up the actual dependency if it receives a call it hasn't seen before.
However, as long as it receives calls it has seen before, it will return whatever was returned the first time around.

## Stateful objects
The technique is intended to memoize highly stateful objects such as databases, so it is assumed that all calls modify the state of the object.

## Example
``` c#
var kvs = new InMemoryKeyValueStore(); // In reality you'd want a persisted store
var rr = RecordReplay<IFoo>.Create(kvs, () => new SlowFoo());
rr.Append("HELLO"); // Our SlowFoo will be created at this point
var output = rr.Append("GOODBYE");

var rr = RecordReplay<IFoo>.Create(kvs, () => new SlowFoo());
rr.Append("HELLO"); // At this point another SlowFoo won't be created, and the cache will be used
var output = rr.Append("GOODBYE");
```

# Mythosia
This project supports custom functions that are not directly provided by .NET as extension methods so that they can be conveniently used within the project. <br/>
The extensions supported by this project include the following. <br/>


## DataStruct
```c#
using Mythosia;
using Mythosia.Collections;

CircularQueue<byte> testQ = new (3);    // create circular queue with max size is 3

testQ.Enqueue(10);  // 10
testQ.Enqueue(5);   // 10 5
testQ.Enqueue(26);   // 10 5 26
testQ.Enqueue(16);   // 16 5 26


// if you want to use a thread-safe circular queue, 
// all you have to do is create a circular queue with the true parameter as below.

CircularQueue<byte> testQ = new (3, true);  // create thread-safe circular queue


```
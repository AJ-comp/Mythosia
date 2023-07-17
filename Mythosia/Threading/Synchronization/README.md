# Mythosia
This project supports custom functions that are not directly provided by .NET as extension methods so that they can be conveniently used within the project. <br/>
The extensions supported by this project include the following. <br/>


## AsyncLock (global)
```c#
using Mythosia;
using Mythosia.Threading.Synchronization;

// assume you have a logic as below and the AllocateUniqueID, WriteLogAsync function have to be executed exclusively.

while(true)
{
	await socket.AcceptAsync();

	await AllocateUniqueID();
	await WriteLogAsync("logging");
}

async AllocateUniqueID()
{
	// (Critical Section) IO exectuion
}

async WriteLogAsync(string data)
{
	// (Critical Section) IO exectuion
}

//but async keyword can't be compatibled with lock keyword.
// in this situation, you can achieve the goal using as below.

while(true)
{
	await socket.AcceptAsync();

	var t1 = AllocateUniqueID;
	t1.ExclusiveAsync();	// execute exclusively

	var t2 = WriteLogAsync;
	t2.ExclusiveAsync("logging");	// execute exclusively
}


```

In the case of the delegate method above is synchronized globally. (use static SemaphoreSlim internally)
In other words, if AllocateUniqueID is executing the WriteLogAsync is not executed until the AllocateUniqueID function is ended.
So, if you want to write code that accesses multiple critical sections independent of each other, the method below is more efficient.


## AsyncLock (local)
```c#
using Mythosia;
using Mythosia.Threading.Synchronization;


var semaID = new SemaphoreSlim(1, 1);
var semaLog = new SemaphoreSlim(1, 1);

while(true)
{
	await socket.AcceptAsync();

	await semaID.ExclusiveAsync(AllocateUniqueID);	// execute exclusively
	await semaLog.ExclusiveAsync(WriteLogAsync, "logging");	// execute exclusively
}


```
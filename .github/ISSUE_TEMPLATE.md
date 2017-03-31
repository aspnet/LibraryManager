### Functional impact
*Does the bug result in any actual functional issue, if so, what?*  
e.g. If poll request starts working again, it recovers. Otherwise, it sends many requests per sec to the servr.

### Minimal repro steps
*What is the smallest, simplest set of steps to reproduce the issue. If needed, provide a project that demonstrates the issue.*  

1. Enable SQL scale out in the samples app
2. Open the Raw connection sample page with long polling transport (~/Raw/?transport=longPolling)
3. Confirm the connection is connected
4. Stop the SQL Server service
5. Wait for the connection to time out (~2 minutes)

### Expected result
*What would you expect to happen if there wasn't a bug*  
e.g. The connection should stop trying to reconnect after the disconnect timeout (~30 seconds)

### Actual result
*What is actually happening*  
e.g. The connection tries reconnecting forever

### Further technical details
*Optional, details of the root cause if known*
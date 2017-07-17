**Project Description**
Simple Windows service to monitor the results of tasks run by the Windows Task Scheduler. Sends a notification if a task executed with an unexpected result.

**Problem**

Since Vista, Microsoft Windows comes with a much improved task scheduler, which is pretty cool. 
However when running many tasks, some of them may fail once in a while. Unless you have reporting built into those tasks it is not easy to find out when they failed. 

You could use the Task Scheduler GUI and review the 'Last Run Result' column, there should be a (0x0) for zero if the tasks ran without problems. Any other number usually indicates a problem. 

But what if the last ran was successful, but the previous one wasn't. You would need to get into the history of the task and browser through all the entries there. 

The Windows event log has an informational entry for each run, whether it was successful or not, so this is also not very helpful unless you want to browse through all event messages and find the ones that failed.

Luckily Microsoft also gave us a much improved API for the new Tasks Scheduler, it is COM based but we can use it from Powershell or DOT.NET to get to the RegisteredTask object which as a property of 'LastTaskResult'

So I wrote a small tool that checks the tasks for a LastTaskResult <> 0 and sends an email to the admin. 
I could have written this as a Powershell script or Console application to run once an hour, but then I would have to schedule it with the same service I want it to monitor. So instead I wrote a Windows Service which runs in the background and checks once an hour. This makes it slightly more complex to install but it is the only other built-in way to run an process regularly.

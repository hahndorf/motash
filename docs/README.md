# Documentation

## Manual Installation

* Extract the files from the zip archive into a directory of your choice
* Open a command prompt (cmd.exe) as administrator in elevated mode (Run as administrator). 
* Navigate into the directory you just created and run the installservice.cmd batch.
* Open the Services console (services.msc) and locate the 'Hacon Motash' service.
* Change the 'Startup Type' to 'Automatic' or 'Automatic (Delayed Start)'
* Make sure on the 'Log on' tab, 'Local System account' is selected.
* Open the 'Hacon.Motash.Service.exe.config' file and adjust it, see below.
* Start the service.
* To uninstall the service, use the second line in the installservice.cmd batch file.
* To test this tool, add a new task pointing to an executable that does not exists. 

## Update to a newer version
* Stop the service
* Copy the new files over the existing ones
* Start the service

## Configuration
The configuration is in the Hacon.Motash.Service.exe.config file which you can edit in Notepad.
* We need to change the email settings. Under MailSettings you need to configure your SMTP server in the host attribute. If your server requires you to log on to send mail, provide a username and password as well.
* tnlSenderEmail is the address used as the sender, AlertRecipient is the address to send the email to. 
* An imported setting is 'RootFolderPattern', it should be the name of the folder you want to monitor. If you created a top level folder 'FooCompany' in Task Schedules, use 'FooCompany' here. If you want to monitor more than one folder use a pipe character to separate them e.g. 'FooCompany|BarInc' you can use a single dot '.' to monitor all folders.
* The setting 'CheckRootTasks' can be 'true' or 'false', as expected, when set to true tasks in the root are also monitored (Version 1.5 and later only)

When you make changes to the configuration, you have to restart the service.

## Some notes
* Even though it runs as a Windows Service, it only checks for failed tasks once an hour.
* When a task that runs once a day fails, Motash would report about it every hour until it may not fail on the next run. Instead Motash only looks at failed tasks that ran since Motash last checked, usually one hour ago.
* Motash ignores tasks that are disabled or are currently running.
* Robocopy.exe returns with Exit codes of 1, 2 or 3 even if the copy operation succeeded. So a failure would be reported by Motash for an exit code of 2. To allow exit codes other than zero, put a comma separated list of integers inside curly brackets anywhere into the description field of the task. For a robocopy task this look like this:

`Copy files, allow exit codes {0,1,2,3}, cool stuff!`

Make sure to include zero. If no such list is found, only zero is considered a 'success' exit code.  

## Known Issues
* Tasks to monitor must be in a folder, not in the root, (from Version 1.5 tasks in the root are monitored).
* Only the top three folder levels are monitored (from Version 1.5 the number of levels supported is 64).
* Some tasks fail and still exit with a return value of zero, there is no way for Motash to detect this. One example is a Powershell script with a syntax error.
* The user has no control over the email content or layout.
* The Motash service has to run under the 'Local System account' or an administrator account which should be avoided as a security measure, but as a normal user I was unable to enumerate the tasks. You can remove privileges, see: [Running with Least Privilege](RunningWithLeastPrivilege.md)
* No MSI installer, I don't like them, but I may do one in the future.
* On Windows Server 2012 Core, there is no .NET Framework 3.5. Motash can be installed but it wont run. To fix this, open the Hacon.Motash.Service.config file and add the following under the configuration node:


    &lt;startup&gt;
      &lt;supportedRuntime version="v4.0" /&gt;
    &lt;/startup&gt;

## Tray Notification
Version 1.5 introduces a small application, that runs in the system tray. 
It checks the tasks every 10 minutes and displays a notification if a task has failed.
You still have to use the Task Scheduler UI to see what went wrong.
Use your preferred way to automatically start Hacon.Motash.Tray.exe

## Notifiers
Starting with version 2.0 I moved the email notification code out of the main assembly into a separate project. I'm now using MEF to support multiple notifiers. 
All that's needed is a single class implementing the **Motash.INotifier** interface. The single method **Send** takes a list of Failures which the notifier can then process in any way it wants.

To create your own notifier, make a copy of the EmailNotifier and adjust it to your needs, then drop the assembly into the bin directory. Then next time you restart the service it should execute your notifier along with others found in the bin directory.

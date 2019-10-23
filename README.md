# IFTTTRunSshCommandTrigger
An IFTTT trigger which allows you to run a command in a SSH connection

This project allows you to execute a command over a SSH prompt as an IFTTT trigger
I personnaly use it to send a wakeonlan command to a raspberry PI inside my home network.

You can create your own IFTTT applet, or use the one available here: https://ifttt.com/applets/HALcRWiC-run-ssh-command-when-i-ask-google-assistant
The IFTTT applet contact an azure function hosted on Azure.


To host the project in your own Azure/IFTTT account, here as the steps required:

1- Connect to https://platform.ifttt.com, and create a new API

2- Launch the plublish wizard in visual studio

3- Be sure to "Edit Azure App Service settings" before publish. Set the "Remote" value of the IFTTT_SERVICE_KEY to the key available in your IFTTT API Managment page

4- Actually publish to Azure

  4.a- You should be able to access the endpoint at https://[YOUR_FUNCTION_NAME].azurewebsites.net/ifttt/v1/actions/run_ssh_command
  
5- Back to IFTTT, create a new action

  5.a- Endpoint will be "run_ssh_command"
  
  5.b- Create 5 actions fields 
  
        -Hostname
        
        -Port
        
        -Username
        
        -Password
        
        -Command
6- Head to the endpoint tests page in IFTTT, and make sure the endpoints still work as expected

Now you can create an applet

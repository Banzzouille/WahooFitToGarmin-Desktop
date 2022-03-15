# Send Wahoo fit files to Garmin Connect - a windows desktop version
A windows app to synchronize fit files coming from Wahoo devices to GarminConnect through dropbox

# The problem i tried to solve
I have been a Garmin device user for a long time. I swithed to Wahoo bike unit after my 1030 died. But unfortunatly, there is no bridge from Wahoo Companion directly to Garmin Connect. I synchronize everything with Strava but use Garmin Connect to check my km done on each bike and componants.

I didn't want to continue to synchronize manually each time i do an activity between these 2 plateforms.

# What you need

- I use auto export functionality to DropBox directly from Wahoo compagnon app
- Install the Dropbox client on your computer
- Install dot net core 3.1 framework from here [.NET Desktop Runtime 3.1.23](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-3.1.23-windows-x64-installer)
- Download and Unzip the app on your computer

# Step by step Guide

## 1. On your wahoo compagnon app
You need to synchronize your Wahoo companion app with your dropbox account. If you don't have a Dropbox account, you can create one here : [https://www.dropbox.com](https://www.dropbox.com)

Then, go to your Wahoo app -> Profile -> Connected Apps -> find dropbox entry in the list -> connect the app to dropbox with your account

![DropBox-Wahoo-App.jpg](https://i.postimg.cc/mknWL7pb/Drop-Box-Wahoo-App.jpg)

Here, when you finish your next activty, a fit file will be directly send to your dropbox cloud account. You can also synchronize your past activities by clicking on "Send training" in the history tab.

## 2. Install Dropbox Client

You can find an installation link [here](https://www.dropbox.com/downloading)

## 3. Install WahooFitToGarmin-Desktop app

Simply unzip the file you donwload on Github where you want.   
Start the application by double click on "WahooFitToGarmin-Desktop.exe"  

Set your personnal information in the settings tab
![Settings.jpg](/Pictures/settings.png)

Then restart the app to take it in account.


File will be discovered when new file appears in the DropBox folder you have selected below
![Example interface.png](/Pictures/Example_interface.png)




## 4. Additionnal actions

To have an autostart app (starting with Windows) you can add a shortcut of the exe file in this folder : C:\Users\%username%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup  

Your data will be stored here : C:\Users\%username%\AppData\Local\WahooFitToGarmin_Desktop


         






All your information will be saved in this folder in CLEAR TEXT.  
If your don't agree with this, DON'T USE THIS APP !!!!


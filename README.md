
forked from [HedgehogDevelopment/SCScheduledPublishing](https://github.com/HedgehogDevelopment/SCScheduledPublishing). 

[Documentation](https://github.com/HedgehogDevelopment/SCScheduledPublishing/tree/master/Documentation) has been updated in the readme markdown. 
# Overview: # 
The purpose of Scheduled Publish is to give the content editor the option to delay the publishing of an item for a future point in time. Thus, a page or a feature that should go live at a specific time can be created and populated in Sitecore and previewed long before it goes live without the risk of an accidental publish before the specific time. Moreover, there is no need for a content-editor to go to Sitecore and manually publish something at an inconvenient hour, e.g. a New Year’s announcement. Scheduled Publish intends to give the content-editor all features of a normal publish with the addition of automation, timing and notifications.
## Source: ##
 - [Source](https://github.com/nehemiahj/SCScheduledPublishing/tree/master/src/Foundation/ScheduledPublish) is updated to Sitecore 10.3.0.   
 - [Sitecore Content Serialization](https://doc.sitecore.com/xp/en/developers/102/developer-tools/sitecore-content-serialization.html) is used to serialize the content. You can use Sitecore CLI to Push and Pull the content. 
## Setup: ##
 1. Package is compatible for Sitecore v10+. You can download it from [here](https://github.com/nehemiahj/SCScheduledPublishing/tree/master/Packages). 
 2. You can take the source and add it to your solution. 
 3. Use the Docker Image from [here](https://hub.docker.com/r/nehemiah/sitecore-scheduled-publish). ```docker pull nehemiah/sitecore-scheduled-publish```
## Features: ##
 - Scheduled Publish
 - Scheduled Unpublish
 - Edit publish schedule
 - Date and time can be customized
 - Warning if the content-editor selects a date that has already passed
 - Check if the item is in a valid publishing state
 - Check if the item’s publishing restrictions allow publishing according to the desired schedule
 - Target database to which to publish
 - Language versions which to publish
 - Publish modes
 - Publish Children
 - Frequency of checks whether there are items queued for publishing
 - Simple interface
 - Customizable email notifications 
## Guide: ##
This is how the Scheduled Publish strip in the Publish Ribbon:

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%201.PNG)

Content-editors can still use the well-known Publish button for all publish methods they are used to. The Schedule Publish button is what is used only for scheduling a future publish.
The Schedule Publish strip consists of three buttons:
 - Schedule Publish button for scheduling a future publishing of the current item
 - Schedule Unpublish button for scheduling a future unpublishing of the current item
 - Edit Schedule button where the content editor can review, edit and delete any of the existing schedules for any items.
## Schedule Publish: ##
This is how the Scheduled Publish dialog looks like:

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%202.PNG)

Please note that the current server time will be used when scheduling and it is indicated in the Scheduled Publish Settings block.

Any existing schedules for the selected item will display in order in the Existing Schedules block. If there are none, this will be indicated.

As you see the input is fairly common to Sitecore’s Publish, with the addition of two dropdown menus.
From the first drop down the content-editors choose a date when to publish. If they choose a date that has passed, they will receive a warning and be returned to the dialog again until they choose a valid date:

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%203.PNG)

From the second dropdown, content-editors should choose an approximate time for publishing. It is approximate since the actual time of publishing will be the time they set +/- the frequency of the check for items for publishing queued.

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%204.PNG)

Sitecore displays only hours and halves, but they can be manually edited afterwards, as long as the time format is kept.

If there are already scheduled publishes for the particular item, a list of these will appear above. The list will show all dates and corresponding hours for publishing for the item in order.

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%205.PNG)

## Schedule Unpublish: ##
The Schedule Unpublish dialog is identical to the Schedule Publish one, only it will remove an item from the website at the selected time.

## Edit Publish: ##
The Edit Schedule button will pop the Edit Scheduled Publishing dialog.

Note: this dialog will list all scheduled publishes by date, not just the scheduled publishes for the currently selected item.

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%206.PNG)

The first column – Item - shows the name of the item and its path, since there may be items with the same name in different locations, especially in a multi-site environment.

The second column – Action – notifies whether the item is scheduled for Publish or Unpublish.

The third column – Date – first shows the current date and time of the schedule, and then two drop down menus for date and time respectively. The first line will not change when you select a different value below – it is just for reference. If you save the new value for any item, the first line will display this new value on reopening the Edit Scheduled Publishing dialog.

The fourth column – Delete – consists of a checkbox. Checking that checkbox will delete the selected schedule upon hitting ‘OK’.

## Publish Notification: ##
Scheduled Publish can be customized to send a notification to the content editor who assigned it and other users when a publish takes place. The email settings can be found under Sitecore/System/Modules/Scheduled Publish.
The Settings item contains a checkbox whether email notifications should be sent upon publish.

![enter image description here](https://raw.githubusercontent.com/nehemiahj/images/main/Publish%207.PNG)

The Scheduled Publish Email Settings item contains mail server info. It can be used to input a mail server or to set a mail server from web.config to be used. Read access to this item can be denied to some content-editors.

The Scheduled Publish Email item contains all fields for a nice, content-managed email.

If notification is enabled, the content-editor who assigned scheduled publish on an item will always receive an email in the mailbox they have input in their Sitecore profile. Additionally, the “To” field can accept a list of comma-separated email addresses. These emails will receive an email on every scheduled publish.

The name of the published item can be added to the Subject of the received email using the “[item]” placeholder for it.

## Email Tokens: ##
There are several placeholders available in the Message field as well so the mail message’s body is very flexible. It can contain anything the content-editor inputs, plus allows the following replacements:

[id] - the id of the item being published

[item] - name of the item being published

[path] - path of the item being published in the content tree

[date] - date when the publishing took place

[time] - the time when the publishing took place

[version] - the version of the item which was published

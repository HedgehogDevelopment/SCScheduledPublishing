<img src="https://www.hhog.com/-/media/PublicImages/Hedgehog/Hedgehog-logo-4color-275x46.jpg" alt="Hedgehog Development" border="0"> 


# Overview: #  

The purpose of Scheduled Publish Module for Sitecore is to give the content editor the option to delay the publishing of an item for a future point in time. The full documentation is available in "Documentation" folder. 
The module supports Sitecore versions: 6.6(.Net Framework 4.0), 7.0, 7.1, 7.2, 7.5 and 8.0  

Blog posts about this feature:   
- [https://spareva.wordpress.com/2015/11/10/sitecore-scheduled-publish-module-content-editors-perspective/
](https://spareva.wordpress.com/2015/11/10/sitecore-scheduled-publish-module-content-editors-perspective/)
- [http://www.sitecorethoughts.com/home/sitecore-scheduled-publish-module-overview
](http://www.sitecorethoughts.com/home/sitecore-scheduled-publish-module-overview
)

Sitecore Marketplace Url: [https://marketplace.sitecore.net/Modules/S/Sitecore_Scheduled_Publish.aspx?sc_lang=en
](https://marketplace.sitecore.net/Modules/S/Sitecore_Scheduled_Publish.aspx?sc_lang=en
)


## Source projects: ##

"ScheduledPublish66" - Source code for Sitecore 6.6  
"ScheduledPublish70-71" - Source code for Sitecore 7.0 and 7.1   
"ScheduledPublish72up" - Source code for Sitecore 7.2, 7.5 and 8.0  


## Setup: ##

1. Clone the project.
2. Copy "Sitecore.Kernel.dll" from your website root into corresponding "Lib" folder depending on the Sitecore version.
3. Configure "Build" section of "Core" and "Master" TDS projects.
4. Right-click the solution and select "Deploy Solution". This will build the code and deploy both the code and TDS items to your Sitecore site.



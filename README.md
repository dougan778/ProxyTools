# ProxyTools

This is a set of .NET projects to assist in spreading HTTP requests across proxies for the purposes of web scraping.  The goal of these libraries together is to collect proxies, vet the effective ones, and provide automatic distribution of requests.

## Getting Started

Create a database using MS Sql Server using the DatabaseTableSetup.sql script in the RequestDistributionDemo project.  Update your appsettings.json and app.config configuration files accordingly to point to this database.  Once this is set up, the next step is to run the ProxyAPI project, which contains a background service that will collect proxies for you to use.  The libraries will throw exceptions if you try to initiate requests without an adequate supply of proxies, so run this and collect proxies first.  Once this is initialized, you can utilize the endpoints provided in the ProxyAPI REST API to retrieve and score proxies.  You can also include the RequestDistribution library in a project and utilize the RequestManager and/or RequestDistributor classes to manage your requests-- the RequestDistributionDemo project demonstrates a simple example of how to do this.

### Prerequisites

You need to have Chrome installed, since requests can use ChromeDriver to execute requests.  You may have to update the version of ChromeDriver  contained in the RequestDistribution project to match your Chrome version.

### Additional Notes

You'll note that this is still a work in progress and there are some aspects which are rather "raw".  One thing to note is that this solution is currently a mixture of .NET Core, .NET Standard, and .NET Framework 4.6.1, which reflects the fact that I have not yet converted my applications using the RequestDistribution library over to .NET Core.

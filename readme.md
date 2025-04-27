This project contains two unrelated learning exercises
* A merge sort implemented with Azure Durable Functions and triggered by an HTTP function trigger. It writes its 
results to Azure Table Storage.  It includes a number of Azure Functions to then interact with the Table Storage
* An Azure SQL customers/orders database, code to populate it with fake data, and an Azure Functions APP to expose
various APIs on the data.
* Both projects include a Bruno (like PostMan) folder which contains a collecition of API definitions, so the APIs
can be tested either locally or from the Azure installation through my APIM instance.
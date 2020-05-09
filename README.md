# Azure_REST_API_Authorization_HMAC
Sample to build the signature to authenticate against Azure REST API using Authorization with HMAC signatures

This sample was tested using the Azure Batch REST API:

https://docs.microsoft.com/en-us/rest/api/batchservice/authenticate-requests-to-the-azure-batch-service

Language: 
C#

Framework:
dotnet core 3.1.0

How to run:
1. Edit the variables in Program.cs
2. dotnet run (https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run)

Change the variables in the beginning of the Program.cs file to match your definition:

    static readonly string AccountName = "YOURACCOUNTNAME";
    static readonly string BaseURL = "https://YOURACCOUNTNAME.REGION.batch.azure.com";
    static readonly string AccountKey = "SHAREDKEY";

    static readonly string RestEndpoint = "jobs";
    static readonly string APIVersion = "2020-03-01.11.0";

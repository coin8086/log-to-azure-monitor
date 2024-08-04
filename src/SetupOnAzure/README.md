# Setup Sample on Azure

The [sample.bicep](sample.bicep) setups a sample cluster of VMs with the [SampleAppWithListener](../SampleAppWithListener/) to show the usage of [LogAnalyticsTraceListener](../TraceListener/), which logs to Azure Monitor in its [Logs Ingestion API](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview). It makes use of the (reusable) module [log-ingestion.bicep](log-ingestion.bicep) to create required Azure resources.

Generally, you need the following things to use the Azure Monitor log ingestion API:

* A Log Analytics workspace. This is where your log goes.
* A custom table in the workspace for your log.
* A Data Collection Rule (DCR), in which you define the schema of your data flow: the format of your incoming message, the table it goes, optionally data transformation, etc.
* A Data Collection End Point (DCE), the URL for the log ingestion API, associated with a DCR.
* A User Managed Identity, who has permission for the DCR (and thus able to write log to Azure Monitor). The identity will be applied to VMs in the sample. (Note that you can use a MS Entra ID App instead, provided that the app has the same required permission as the identity. But you need to make a little change to LogAnalyticsTraceListener to accept the Entra app.)

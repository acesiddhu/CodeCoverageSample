using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.TestClient.PublishTestResults;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace CodeCoverageAttchment
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //System.Diagnostics.Debugger.Launch();
                string collectionUrl = args[0]; // $(System.TeamFoundationCollectionUri)
                var connection = new VssConnection(new Uri(collectionUrl), new Microsoft.VisualStudio.Services.OAuth.VssOAuthAccessTokenCredential(args[4]));

                var testClient = new TestManagementHttpClient(new Uri(collectionUrl), new Microsoft.VisualStudio.Services.OAuth.VssOAuthAccessTokenCredential(args[4]));

                string buildurl = args[1]; // $(BUILD.BUILDURI)
                string projectName = args[2]; // $(SYSTEM.TEAMPROJECT)
                string folder = args[3];
                int buildId = int.Parse(args[5]); //$(Build.BuildId)
                bool useNewStore = false;
                string projectId = string.Empty;

                try
                {
                    bool.TryParse(args[6], out useNewStore);
                    projectId = args[7]; //$(System.TeamProjectId)
                }
                catch(Exception e)
                {

                }

                // Create test run
                RunCreateModel runCreationModel = new RunCreateModel("somename", buildId: buildId,
                    startedDate: DateTime.UtcNow.ToString(), completedDate: DateTime.UtcNow.ToString(),
                    state: TestRunState.InProgress.ToString(), isAutomated: true);
                var testRun = testClient.CreateTestRunAsync(runCreationModel, projectName).Result;

                TestCaseResult result = new TestCaseResult
                {
                    AutomatedTestName = "TestOne",
                    AutomatedTestStorage = "foo.dll",
                    Build = new ShallowReference { Url = buildurl },
                    TestCaseTitle = "TestOne"
                };
                testClient.AddTestResultsToTestRunAsync(new TestCaseResult[] { result }, projectName, testRun.Id).Wait();
                testClient.UpdateTestRunAsync(new RunUpdateModel("somename", state: TestRunState.Completed.ToString()), projectName, testRun.Id).Wait();

                if (useNewStore)
                {
                    UploadCodeCoverageAttachmentsToNewStore(folder, connection, buildId, projectId);
                }
                else
                {
                    UploadCodeCoverageAttachmentsToOldStore(testClient, buildurl, projectName, folder);
                }
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e);
            }
        }

        private static void UploadCodeCoverageAttachmentsToNewStore(string folder, VssConnection connection, int buildId, string projectId)
        {
            TestLogStore logStore = new TestLogStore(connection, new LogTraceListener());
            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (var file in Directory.EnumerateFiles(folder, "*.coverage", SearchOption.AllDirectories))
            {
                if (files.TryGetValue(Path.GetFileName(file), out string something))
                {
                    continue;
                }
                else
                {
                    files.Add(Path.GetFileName(file), file);
                    Dictionary<string, string> metaData = new Dictionary<string, string>();
                    metaData.Add("ModuleName", Path.GetFileName(file));
                    var attachment = logStore.UploadTestBuildLogAsync(new Guid(projectId), buildId, Microsoft.TeamFoundation.TestManagement.WebApi.TestLogType.Intermediate, file, metaData, null, false, System.Threading.CancellationToken.None).Result;
                }
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.coveragebuffer", SearchOption.AllDirectories))
            {
                Dictionary<string, string> metaData = new Dictionary<string, string>();
                metaData.Add("ModuleName", Path.GetFileName(file));
                var attachment = logStore.UploadTestBuildLogAsync(new Guid(projectId), buildId, Microsoft.TeamFoundation.TestManagement.WebApi.TestLogType.Intermediate, file, metaData, null, true, System.Threading.CancellationToken.None).Result;
            }
        }

        private static void UploadCodeCoverageAttachmentsToOldStore(TestManagementHttpClient testClient, string buildurl, string projectName, string folder)
        {
            var testRuns = testClient.GetTestRunsAsync(projectName, buildurl).Result;

            foreach (var t in testRuns)
            {
                int testRunId = t.Id;

                Dictionary<string, string> files = new Dictionary<string, string>();

                // one meta data per module
                foreach (var file in Directory.EnumerateFiles(folder, "*.coverage", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);

                    if (!files.ContainsKey(fileName))
                    {
                        var attachment = GetAttachmentRequestModel(file);
                        Task<TestAttachmentReference> trTask = testClient.CreateTestRunAttachmentAsync(attachment, projectName, testRunId);
                        trTask.Wait();
                        files.Add(fileName, file);
                    }
                }

                // buffer
                foreach (var file in Directory.EnumerateFiles(folder, "*.coveragebuffer", SearchOption.AllDirectories))
                {
                    var attachment = GetAttachmentRequestModel(file);

                    Task<TestAttachmentReference> trTask = testClient.CreateTestRunAttachmentAsync(attachment, projectName, testRunId);
                    trTask.Wait();
                }
            }
        }

        const int TCM_MAX_FILESIZE = 104857600;

        public static TestAttachmentRequestModel GetAttachmentRequestModel(string attachment)
        {
            if (File.Exists(attachment) && new FileInfo(attachment).Length <= TCM_MAX_FILESIZE)
            {
                byte[] bytes = File.ReadAllBytes(attachment);
                string encodedData = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
                if (encodedData.Length <= TCM_MAX_FILESIZE)
                {
                    return new TestAttachmentRequestModel(encodedData, Path.GetFileName(attachment), "", AttachmentType.IntermediateCollectorData.ToString());
                }
                else
                {
                    Console.WriteLine("size");
                }
            }
            else
            {
                Console.WriteLine("size");
            }

            return null;
        }
    }

    public class LogTraceListener : TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            switch (eventType)
            {
                case TraceEventType.Information:
                    Console.WriteLine(message);
                    break;
                case TraceEventType.Warning:
                    Console.WriteLine(message);
                    break;
                case TraceEventType.Verbose:
                    Console.WriteLine(message);
                    break;
            }
        }

        public override void Write(string message)
        {
            Console.WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}

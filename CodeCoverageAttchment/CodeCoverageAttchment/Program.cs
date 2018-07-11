using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CodeCoverageAttchment
{
    class Program
    {
        static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            string collectionUrl = args[0];

            var testClient = new TestManagementHttpClient(new Uri(collectionUrl), new Microsoft.VisualStudio.Services.OAuth.VssOAuthAccessTokenCredential(args[4]));

            string buildurl = args[1];
            string projectName = args[2];
            string folder = args[3];

            // Create test run
            RunCreateModel runCreationModel = new RunCreateModel("somename", buildId: int.Parse(args[5]),
                startedDate: DateTime.UtcNow.ToString(), completedDate: DateTime.UtcNow.ToString(),
                state: TestRunState.InProgress.ToString());
            var testRun = testClient.CreateTestRunAsync(runCreationModel, projectName).Result;

            //TestCaseResult result = new TestCaseResult { AutomatedTestName = "TestOne", AutomatedTestStorage = "foo.dll", Build = new ShallowReference { Url = buildurl},  }
            //testClient.AddTestResultsToTestRunAsync()

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

        private static VssConnection CreateServerConnection()
        {
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(TfsTeamProjectCollection.GetFullyQualifiedUriForName("http://localhost:8080/tfs/Defaultcollection"));
            VssCredentials credentials = new VssClientCredentials(useDefaultCredentials: true);
            credentials.Storage = new VssClientCredentialStorage();
            credentials.PromptType = CredentialPromptType.PromptIfNeeded;
            VssConnection connection = new VssConnection(tfs.Uri, credentials);

            return connection;
        }
    }
}

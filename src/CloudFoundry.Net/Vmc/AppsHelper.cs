﻿namespace CloudFoundry.Net.Vmc
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using CloudFoundry.Net.Types;
    using ICSharpCode.SharpZipLib.Zip;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestSharp;

    public class AppsHelper : BaseVmcHelper
    {
#if UNUSED
        public string Start(string argName) // TODO error return, require login?
        {
            RestClient client = buildClient();
            RestRequest request = buildRequest(Method.PUT, String.Format("{0}/{1}", Constants.APPS_PATH, argName));
            return executeRequest(client, request);
        }
#endif

        public void Start(Cloud argCloud, Application argApplication)
        {
            argApplication.State = Instance.InstanceState.STARTED;
            UpdateApplicationSettings(argCloud, argApplication);
        }

        public void Stop(Cloud argCloud, Application argApplication)
        {
            argApplication.State = Instance.InstanceState.STOPPED;
            UpdateApplicationSettings(argCloud, argApplication);
        }

        public string GetAppInfo(string argName)
        {
            RestClient client = buildClient();
            RestRequest request = buildRequest(Method.GET, Constants.APPS_PATH, argName);
            return client.Execute(request).Content;
        }

        public Application GetAppInfo(Cloud argCloud, String argName)
        {
            RestClient client = buildClient(argCloud);
            RestRequest request = buildRequest(Method.GET, Constants.APPS_PATH, argName);
            return executeRequest<Application>(client, request);
        }

        public VcapResponse UpdateApplicationSettings(Cloud argCloud, Application argApplication)
        {
            RestClient client = buildClient(argCloud);
            RestRequest request = buildRequest(Method.PUT, DataFormat.Json, Constants.APPS_PATH, argApplication.Name);
            request.AddBody(argApplication);
            return executeRequest<VcapResponse>(client, request);
        }

        public string DeleteApp(string appname, string url, string accesstoken)
        {
            if (url == null)
            {
                return ("Target URL has to be set");
            }
            else if (accesstoken == null)
            {
                return ("Please login first");
            }
            else
            {
                var client = new RestClient();
                client.BaseUrl = url;
                var request = new RestRequest();
                request.Method = Method.DELETE;
                request.Resource = "/apps/" + appname;
                request.AddHeader("Authorization", accesstoken);
                return client.Execute(request).Content;
            }
        }

        public void DeleteApp(Application application, Cloud cloud)
        {
            var client = new RestClient();
            client.BaseUrl = cloud.Url;
            var request = new RestRequest();
            request.Method = Method.DELETE;
            request.Resource = "/apps/" + application.Name;
            request.AddHeader("Authorization", cloud.AccessToken);
            client.Execute(request);
        }

        public void RestartApp(Application application, Cloud cloud)
        {
            Stop(cloud, application);
            Start(cloud, application);
        }

        public string Push(string argName, DirectoryInfo argPath, string argDeployUrl,
            string argFramework, string argRuntime, uint argMemoryReservation, string argServiceBindings)
        {
            if (argPath == null)
            {
                return ("Application local location is needed");
            }
            else if (argDeployUrl == null)
            {
                return ("Please specify the url to deploy as.");
            }
            else if (argFramework == null)
            {
                return ("Please specify application framework");
            }
            else
            {
                /*
                 * Before creating the app, ensure we can build resource list
                 */
                var resources = new List<Resource>();
                addDirectoryToResources(resources, argPath, argPath.FullName);

                if (argServiceBindings == null)
                {
                    argServiceBindings = "none";
                }

                var manifest = new AppManifest
                {
                    Name = argName,
                    Staging = new Staging { Framework = argFramework, Runtime = argRuntime },
                    Uris = new string[] { argDeployUrl },
                    Instances = 1,
                    Resources = new AppResources { Memory = argMemoryReservation },
                };

                RestClient client = buildClient();
                RestRequest request = buildRequest(Method.POST, DataFormat.Json, Constants.APPS_PATH);
                request.AddBody(manifest);
                RestResponse response = executeRequest(client, request);

                // This is required in order to pass the JSON as a parameter
                string resourcesJson = JsonConvert.SerializeObject(resources.ToArrayOrNull());

                client = buildClient();
                request = buildRequest(Method.POST, DataFormat.Json, Constants.RESOURCES_PATH);
                request.AddBody(resourcesJson);
                response = executeRequest(client, request);

                client = buildClient();

                string tempFile = Path.GetTempFileName();
                try
                {
                    var zipper = new FastZip();
                    zipper.CreateZip(tempFile, argPath.FullName, true, String.Empty);

                    request = buildRequest(Method.POST, Constants.APPS_PATH, argName, "application");
                    request.AddParameter("_method", "put");
                    request.AddFile("application", tempFile);
                    request.AddParameter("resources", resourcesJson);

                    response = executeRequest(client, request);
                }
                finally
                {
                    File.Delete(tempFile);
                }

                string app = GetAppInfo(argName);
                JObject getInfo = JObject.Parse(app);
                getInfo["state"] = "STARTED";

                client = buildClient();

                request = buildRequest(Method.PUT, DataFormat.Json, Constants.APPS_PATH, argName);
                request.AddBody(getInfo);
                response = client.Execute(request);

                string info = string.Empty;
                for (int i = 0; i < 4; i++)
                {
                    info = GetAppInfo(argName);
                    var crash = GetAppCrash(argName);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }

                return info;
            }
        }

        public string GetAppCrash(string argName)
        {
            RestClient client = buildClient();
            RestRequest request = buildRequest(Method.GET, Constants.APPS_PATH, argName, "crashes");
            RestResponse response = executeRequest(client, request);
            return response.Content;
        }

        public IEnumerable<Crash> GetAppCrash(Application argApplication, Cloud argCloud)
        {
            RestClient client = buildClient(argCloud);
            RestRequest request = buildRequest(Method.GET, Constants.APPS_PATH, argApplication.Name, "crashes");
            return executeRequest<List<Crash>>(client, request);
        }

#if UNUSED
        public string ListApps(string url, string accesstoken)
        {
            if (url == null)
            {
                return ("Target URL has to be set");
            } 
            else if (accesstoken == null)
            {
                return ("Please login first");
            }
            else
            {
                var client = new RestClient();
                client.BaseUrl = url;
                var request = new RestRequest();
                request.Method = Method.GET;
                request.Resource = "/apps";
                request.AddHeader("Authorization", accesstoken);
                return client.Execute(request).Content;
            }
        }
#endif

        public IEnumerable<Application> ListApps(Cloud argCloud)
        {
            RestClient client = buildClient(argCloud);

            RestRequest request = buildRequest(Method.GET, Constants.APPS_PATH);

            IEnumerable<Application> rv = executeRequest<List<Application>>(client, request);

            if (null != rv)
            {
                foreach (Application app in rv)
                {
                    app.Parent = argCloud; // TODO
                }
            }

            return rv;
        }

        private static void addDirectoryToResources(List<Resource> argResources, DirectoryInfo argDirectory, string argRootFullName)
        {
            var fileTrimStartChars = new[] { '\\', '/' };

            foreach (FileInfo file in argDirectory.GetFiles())
            {
                string hash     = generateHash(file.FullName);
                long size       = file.Length;
                string filename = file.FullName;
                // The root path should be stripped. This is used
                // by the server to TAR up the file that gets pushed
                // to the DEA.
                filename = filename.Replace(argRootFullName, String.Empty);
                filename = filename.TrimStart(fileTrimStartChars);
                filename = filename.Replace('\\', '/');
                argResources.Add(new Resource((ulong)file.Length, hash, filename));
            }

            foreach (var subdirectory in argDirectory.GetDirectories())
            {
                addDirectoryToResources(argResources, subdirectory, argRootFullName);
            }
        }

        private static string generateHash(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                using (var sha1 = new SHA1Managed())
                {
                    return BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", String.Empty).ToLowerInvariant();
                }
            }
        }
    }
}
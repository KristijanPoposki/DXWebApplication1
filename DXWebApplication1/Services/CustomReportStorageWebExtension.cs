﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.XtraReports.UI;
using DXWebApplication1.PredefinedReports;

namespace DXWebApplication1.Services
{
    public class CustomReportStorageWebExtension : DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension
    {
        const string FileExtension = ".repx";
        readonly string reportDirectory;

        public CustomReportStorageWebExtension(string reportDirectory) {
            this.reportDirectory = Path.GetFullPath(reportDirectory);
            if (!Directory.Exists(this.reportDirectory)) {
                Directory.CreateDirectory(this.reportDirectory);
            }
        }

        public override bool CanSetData(string url) {
            // Determines whether a report with the specified URL can be saved.
            // Add custom logic that returns **false** for reports that should be read-only.
            // Return **true** if no valdation is required.
            // This method is called only for valid URLs (if the **IsValidUrl** method returns **true**).

            return true;
        }

        public override bool IsValidUrl(string url) {
            // Determines whether the URL passed to the current report storage is valid.
            // Implement your own logic to prohibit URLs that contain spaces or other specific characters.
            // Return **true** if no validation is required.

            return Path.GetFileName(url) == url;
        }

        public override byte[] GetData(string url) {
            // Uses a specified URL to return report layout data stored within a report storage medium.
            // This method is called if the **IsValidUrl** method returns **true**.
            // You can use the **GetData** method to process report parameters sent from the client
            // if the parameters are included in the report URL's query string.
            try {
                if (Directory.EnumerateFiles(reportDirectory).Select(Path.GetFileNameWithoutExtension).Contains(url))
                {
                    return File.ReadAllBytes(Path.Combine(reportDirectory, url + FileExtension));
                }
                if (ReportsFactory.Reports.ContainsKey(url))
                {
                    using (MemoryStream ms = new MemoryStream()) {
                        ReportsFactory.Reports[url]().SaveLayoutToXml(ms);
                        return ms.ToArray();
                    }
                }
            } catch (Exception) {
                throw new FaultException(new FaultReason("Could not get report data."), new FaultCode("Server"), "GetData");
            }
            throw new FaultException(new FaultReason(string.Format("Could not find report '{0}'.", url)), new FaultCode("Server"), "GetData");
        }

        public override Dictionary<string, string> GetUrls() {
            // Returns a dictionary that contains the report names (URLs) and display names. 
            // The Report Designer uses this method to populate the Open Report and Save Report dialogs.
            
            return Directory.GetFiles(reportDirectory, "*" + FileExtension)
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .Union(ReportsFactory.Reports.Select(x => x.Key))
                                     .ToDictionary<string, string>(x => x);
        }

        public override void SetData(XtraReport report, string url) {
            // Saves the specified report to the report storage with the specified name
            // (saves existing reports only). 

            var resolvedUrl = Path.GetFullPath(Path.Combine(reportDirectory, url + FileExtension));
            if(!resolvedUrl.StartsWith(reportDirectory + Path.DirectorySeparatorChar)) {
                throw new FaultException(new FaultReason("Invalid report name."), new FaultCode("Server"), "SetData");
            }

            report.SaveLayoutToXml(resolvedUrl);
        }

        public override string SetNewData(XtraReport report, string defaultUrl) {
            // Allows you to validate and correct the specified name (URL).
            // This method also allows you to return the resulting name (URL),
            // and to save your report to a storage. The method is called only for new reports.
            SetData(report, defaultUrl);
            return defaultUrl;
        }
    }
}
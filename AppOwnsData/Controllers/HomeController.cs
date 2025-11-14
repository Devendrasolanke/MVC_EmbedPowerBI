// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace PowerBIEmbedded_AppOwnsData.Controllers
{
    using AppOwnsData.Models;
    using AppOwnsData.Services;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Web.Mvc;

    public class HomeController : Controller
    {
        private readonly string connectionString = ConfigurationManager.AppSettings["ConnectionString"].ToString();
        public string strsql = "";
        CommonUtility _commonUtility = new CommonUtility();

        public HomeController()
        {
            //m_errorMessage = ConfigValidatorService.GetWebConfigErrors();
        }

        public ActionResult Index()
        {
            // Assembly info is not needed in production apps and the following 6 lines can be removed

            var result = new IndexConfig();
            var assembly = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(n => n.Name.Equals("Microsoft.PowerBI.Api")).FirstOrDefault();
            if (assembly != null)
            {
                result.DotNetSDK = assembly.Version.ToString(3);
            }
            return View(result);
        }

        public async Task<ActionResult> EmbedReport(string reportId)
        {
            var userId = Session["UserID"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.UserID = userId;

            string strsql = $@"
        SELECT distinct a.UserEmail, b.RepID, b.ReportName, b.ReportID
        FROM PB_Users a 
        INNER JOIN PB_Reports b ON a.RepID = b.RepID 
        WHERE a.UserEmail = '{userId}' AND Active = 'Y'";

            DataTable dt = _commonUtility.OpenDataTable(connectionString, strsql);

            if (dt == null || dt.Rows.Count == 0)
            {
                Session.Clear();
                return RedirectToAction("Index", "Login");
            }

            // ✅ Determine which report ID to use (convert string to Guid)
            string selectedReportIdStr = reportId ?? dt.Rows[0]["ReportID"].ToString();

            if (!Guid.TryParse(selectedReportIdStr, out Guid selectedReportGuid))
            {
                // Invalid GUID → fallback to first valid one in table
                if (!Guid.TryParse(dt.Rows[0]["ReportID"].ToString(), out selectedReportGuid))
                {
                    Session.Clear();
                    return RedirectToAction("Index", "Login");
                }
            }

            /*try
            {
                // ✅ Pass Guid to EmbedService
                var embedResult = await EmbedService.GetEmbedParams(
                    ConfigValidatorService.WorkspaceId,
                    selectedReportGuid
                );

                ViewBag.ReportTable = dt;
                ViewBag.SelectedReportId = selectedReportGuid.ToString();

                return View(embedResult);
            }
            catch (Exception)
            {
                Session.Clear();
                return RedirectToAction("Index", "Login");
            }*/

            int maxRetries = 5;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var embedResult = await EmbedService.GetEmbedParams(
                        ConfigValidatorService.WorkspaceId,
                        selectedReportGuid
                    );

                    ViewBag.ReportTable = dt;
                    ViewBag.SelectedReportId = selectedReportGuid.ToString();

                    return View(embedResult);
                }
                catch (Exception)
                {
                    if (attempt == maxRetries)
                    {
                        // ✅ Final failure: redirect to login or show error
                        Session.Clear();
                        return RedirectToAction("Index", "Login");
                    }
                }
            }
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public async Task<JsonResult> GetReportEmbedData(string reportId)
        {
            try
            {
                // ✅ 1. Check session (optional but recommended)
                if (Session["UserID"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session expired. Please login again."
                    },
                    JsonRequestBehavior.AllowGet);
                }

                var userId = Session["UserID"] as string;

                if (!Guid.TryParse(reportId, out Guid reportGuid))
                    return Json(new { success = false, message = "Invalid report ID" }, JsonRequestBehavior.AllowGet);

                string strsql = $"select a.ReportFilter, b.Plant from PB_Reports a, PB_Users b where a.RepID = b.RepID and a.ReportID = '{reportId}' and UserEmail = '{userId}' and Active = 'Y'";
                DataTable dt = _commonUtility.OpenDataTable(connectionString, strsql);

                // Build table/column and values
                string tableName = null;
                string columnName = null;
                var values = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    var rf = row["ReportFilter"]?.ToString().Trim();
                    var plant = row["Plant"]?.ToString().Trim();

                    if (string.IsNullOrEmpty(rf) || string.IsNullOrEmpty(plant))
                        continue;

                    // Expecting format like "T001/Company_Code"
                    var parts = rf.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        tableName = parts[0];
                        columnName = parts[1];
                    }
                    else
                    {
                        // If format unexpected, fallback: put whole value in columnName so upstream can decide
                        columnName = rf;
                    }

                    if (!values.Contains(plant))
                        values.Add(plant);
                }

                var embedResult = await EmbedService.GetEmbedParams(
                    ConfigValidatorService.WorkspaceId,
                    reportGuid
                );

                // Return minimal filter info (table, column, values)
                return Json(new
                {
                    success = true,
                    token = embedResult.EmbedToken.Token,
                    embedUrl = embedResult.EmbedReports[0].EmbedUrl,
                    reportId = embedResult.EmbedReports[0].ReportId,
                    filter = new
                    {
                        table = tableName,
                        column = columnName,
                        values = values
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}

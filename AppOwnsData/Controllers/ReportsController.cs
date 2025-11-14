using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using AppOwnsData.Models;

public class ReportsController : Controller
{
    private readonly string connectionString = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];

    public ActionResult Index()
    {
        ViewBag.ActiveMenu = "Reports";

        List<ReportsMaster> list = new List<ReportsMaster>();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM PB_Reports", con);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new ReportsMaster
                {
                    RepID = Convert.ToInt32(dr["RepID"]),
                    ReportName = dr["ReportName"].ToString(),
                    ReportID = dr["ReportID"].ToString(),
                    ReportFilter = dr["ReportFilter"].ToString()
                });
            }
        }

        return View(list);
    }

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Create(ReportsMaster model)
    {
        CommonUtility _commonUtility = new CommonUtility();
        try
        {
            DataTable dt = _commonUtility.OpenDataTable(connectionString, $"Select * From PB_Reports Where ReportID = '{model.ReportID}'");
            if(dt.Rows.Count > 0)
            {
                // ✅ ERROR POPUP
                TempData["popupType"] = "error";
                TempData["popupMessage"] = "Report already exist!";
                return RedirectToAction("Create", new { section = "reports" });
            }
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO PB_Reports (ReportName, ReportID, ReportFilter) VALUES (@N, @I, @F)",
                    con);

                cmd.Parameters.AddWithValue("@N", model.ReportName);
                cmd.Parameters.AddWithValue("@I", model.ReportID);
                cmd.Parameters.AddWithValue("@F", model.ReportFilter);

                cmd.ExecuteNonQuery();
            }
            // ✅ SUCCESS POPUP
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "Report added successfully!";
            return RedirectToAction("Create", new { section = "reports" });
        }
        catch (Exception)
        {
            // ✅ ERROR POPUP
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "Something went wrong, report not added!";
            return RedirectToAction("Create", new { section = "reports" });
        }        
    }

    public ActionResult Edit(int id)
    {
        ViewBag.ActiveMenu = "Reports";
        ReportsMaster rm = new ReportsMaster();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand(
                "SELECT * FROM PB_Reports WHERE RepID=@ID", con);

            cmd.Parameters.AddWithValue("@ID", id);

            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                rm.RepID = Convert.ToInt32(dr["RepID"]);
                rm.ReportName = dr["ReportName"].ToString();
                rm.ReportID = dr["ReportID"].ToString();
                rm.ReportFilter = dr["ReportFilter"].ToString();
            }
        }

        return View(rm);
    }

    [HttpPost]
    public ActionResult Edit(ReportsMaster model)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE PB_Reports SET ReportName=@Name, ReportID=@RID, ReportFilter=@Filter " +
                    "WHERE RepID=@ID", con);

                cmd.Parameters.AddWithValue("@Name", model.ReportName);
                cmd.Parameters.AddWithValue("@RID", model.ReportID);
                cmd.Parameters.AddWithValue("@Filter", model.ReportFilter);
                cmd.Parameters.AddWithValue("@ID", model.RepID);

                cmd.ExecuteNonQuery();
            }
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "Report updated successfully!";
            return RedirectToAction("Index", "Admin", new { section = "reports" });
        }
        catch (Exception)
        {
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "Something went wrong, report not updated!";
            return RedirectToAction("Index", "Admin", new { section = "reports" });
        }        
    }

    public ActionResult Delete(int id)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM PB_Reports WHERE RepID=@ID", con);
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.ExecuteNonQuery();
            }
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "Report deleted successfully!";
            return RedirectToAction("Index", "Admin", new { section = "reports" });
        }
        catch (Exception)
        {
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "Something went wrong, report not deleted!";
            return RedirectToAction("Index", "Admin", new { section = "reports" });
        }        
    }

}

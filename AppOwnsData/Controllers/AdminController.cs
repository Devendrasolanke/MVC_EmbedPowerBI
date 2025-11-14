using AppOwnsData.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

public class AdminController : Controller
{
    private readonly string connectionString = ConfigurationManager.AppSettings["ConnectionString"].ToString();
    CommonUtility _commonUtility = new CommonUtility();

    public AdminController()
    {
    }

    // ✅ LIST ALL USERS
    public ActionResult Index()
    {
        List<UserAccessMaster> list = new List<UserAccessMaster>();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string StrCond = "";
            con.Open();
            if (Session["UserID"] != null)
            {
                StrCond = $" WHERE UserEmail != '{Session["UserID"].ToString()}'";
            }
            else
            {
                Session.Clear();
                return RedirectToAction("Index","Login");
            }
            SqlCommand cmd = new SqlCommand($"SELECT U.*, R.ReportName FROM PB_Users U LEFT JOIN PB_Reports R ON U.RepID = R.RepID" + StrCond, con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                list.Add(new UserAccessMaster
                {
                    SrNo = Convert.ToInt32(dr["SrNo"]),
                    UserEmail = dr["UserEmail"].ToString(),
                    RepID = Convert.ToInt32(dr["RepID"]),
                    ReportName = dr["ReportName"].ToString(),
                    UserPassHash = dr["UserPass"].ToString(),
                    Plant = dr["Plant"].ToString(),
                    Active = dr["Active"].ToString()
                });
            }
        }

        string sql = "SELECT * FROM PB_Reports";
        ViewBag.ReportTable = _commonUtility.OpenDataTable(connectionString, sql);

        return View(list);
    }

    // ✅ CREATE PAGE
    public ActionResult Create()
    {
        string sql = "SELECT RepID, ReportName FROM PB_Reports";
        ViewBag.ReportList = _commonUtility.OpenDataTable(connectionString, sql);
        return View();
    }

    // ✅ CREATE USER (SAVE)
    [HttpPost]
    public ActionResult Create(UserAccessMaster model, string UserPass)
    {
        string InitialPass = "";
        bool IsInitialPass = false;

        // ✅ Check if user with same combination exists
        DataTable dtt = _commonUtility.OpenDataTable(connectionString,
            $"select * from PB_Users where UserEmail = '{model.UserEmail}' and RepID = '{model.RepID}' and Plant = '{model.Plant}'");

        if (dtt.Rows.Count > 0)
        {
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "This entry already exists!";
            return RedirectToAction("Create");
        }

        // ✅ Check existing password
        DataTable dt = _commonUtility.OpenDataTable(connectionString,
            $"Select UserPass, PassRstDt From PB_Users Where UserEmail = '{model.UserEmail}'");

        if (dt.Rows.Count > 0)
        {
            model.UserPassHash = dt.Rows[0]["UserPass"].ToString();
            model.PassRstDt = dt.Rows[0]["PassRstDt"].ToString();
        }
        else
        {
            IsInitialPass = true;
            InitialPass = _commonUtility.InitialPassGeneration();
            model.UserPassHash = _commonUtility.Encrypt(InitialPass);
            model.PassRstDt = DateTime.Now.ToString("yyyyMMdd");
        }

        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    $"INSERT INTO PB_Users (UserEmail, RepID, UserPass, Plant, Active, PassRstDt, IsDefault) VALUES (@U, @R, @P, @PL, @A, @PR, @ID)",
                    con);

                cmd.Parameters.AddWithValue("@U", model.UserEmail);
                cmd.Parameters.AddWithValue("@R", model.RepID);
                cmd.Parameters.AddWithValue("@P", model.UserPassHash);
                cmd.Parameters.AddWithValue("@PL", model.Plant);
                cmd.Parameters.AddWithValue("@A", model.Active);
                cmd.Parameters.AddWithValue("@PR", model.PassRstDt);
                cmd.Parameters.AddWithValue("@ID", IsInitialPass ? "Y" : "N");

                cmd.ExecuteNonQuery();
            }

            if (IsInitialPass)
            {
                _commonUtility.ResetPass(model.UserEmail, InitialPass);
            }

            // ✅ SUCCESS POPUP
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "User added successfully!";
            return RedirectToAction("Create");
        }
        catch (Exception)
        {
            // ✅ ERROR POPUP
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "An error occurred while adding new user!";
            return RedirectToAction("Index");
        }
    }

    // ✅ EDIT PAGE
    public ActionResult Edit(int id)
    {
        UserAccessMaster data = new UserAccessMaster();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("SELECT * FROM PB_Users WHERE SrNo=@ID", con);
            cmd.Parameters.AddWithValue("@ID", id);

            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                data.SrNo = Convert.ToInt32(dr["SrNo"]);
                data.UserEmail = dr["UserEmail"].ToString();
                data.RepID = Convert.ToInt32(dr["RepID"]);
                data.Plant = dr["Plant"].ToString();
                data.Active = dr["Active"].ToString();
            }
        }

        // ✅ Load report list for dropdown
        string sql = "SELECT RepID, ReportName FROM PB_Reports";
        ViewBag.ReportList = _commonUtility.OpenDataTable(connectionString, sql);

        return View(data);
    }

    // ✅ UPDATE USER (SAVE)
    [HttpPost]
    public ActionResult Edit(UserAccessMaster model, string NewPassword)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd;

                cmd = new SqlCommand(
                        @"UPDATE PB_Users 
                      SET UserEmail=@U, RepID=@R, Plant=@PL, Active=@A
                      WHERE SrNo=@ID", con);

                cmd.Parameters.AddWithValue("@U", model.UserEmail);
                cmd.Parameters.AddWithValue("@R", model.RepID);
                cmd.Parameters.AddWithValue("@PL", model.Plant);
                cmd.Parameters.AddWithValue("@A", model.Active);
                cmd.Parameters.AddWithValue("@ID", model.SrNo);

                cmd.ExecuteNonQuery();
            }
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "User updated successfully!";
            /*return RedirectToAction("Edit");*/
            TempData["popupRedirect"] = Url.Action("Index", "Admin");

            return RedirectToAction("Edit", new { id = model.SrNo });
        }
        catch (Exception)
        {
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "Something went wrong, user not updated!";
            return RedirectToAction("Edit");
        }       
    }

    // ✅ DELETE USER
    public ActionResult Delete(int id)
    {
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM PB_Users WHERE SrNo=@ID", con);
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
            TempData["popupType"] = "success";
            TempData["popupMessage"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            TempData["popupType"] = "error";
            TempData["popupMessage"] = "Something went wrong, user not deleted!";
            return RedirectToAction("Index");
        }
        
    }

    [HttpPost]
    public ActionResult ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            TempData["msg"] = "Email required!";
            return RedirectToAction("Index", new { section = "forgot" });
        }

        DataTable dt = _commonUtility.OpenDataTable(connectionString, $"Select * From PB_Users Where UserEmail = '{email}'");
        if (dt.Rows.Count == 0)
        {
            TempData["msg"] = "User not found, please enter valid email id!";
            return RedirectToAction("Index", new { section = "forgot" });
        }

        string InitialPass = _commonUtility.InitialPassGeneration();
        string EncryptPass = _commonUtility.Encrypt(InitialPass);

        int result = _commonUtility.ExecuteNonQuery(connectionString, $"Update PB_Users Set UserPass = '{EncryptPass}', IsDefault = 'Y', PassRstDt = '{DateTime.Now.ToString("yyyyMMdd")}' Where UserEmail = '{email}'");

        if (result > 0)
        {
            bool IsMailSent = _commonUtility.ResetPass(email, InitialPass);
            if (IsMailSent)
            {
                TempData["msg"] = "Password reset successfully!";
            }
            else
            {
                TempData["msg"] = "Something went wrong, password not reset!";
            }
        }
        else
        {
            TempData["msg"] = "Something went wrong, password not reset!";            
        }
        return RedirectToAction("Index", new { section = "forgot" });
    }
}
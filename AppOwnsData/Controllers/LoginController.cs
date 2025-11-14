using AppOwnsData.Models;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace AppOwnData.Controllers
{
    public class LoginController : Controller
    {
        private readonly string connectionString = ConfigurationManager.AppSettings["ConnectionString"].ToString();
        public string strsql = "";

        [HttpGet]
        public ActionResult Index()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        public ActionResult Index(LoginModel model)
        {
            CommonUtility util = new CommonUtility();

            if (string.IsNullOrEmpty(model.UserID) || string.IsNullOrEmpty(model.Password))
            {
                model.ErrorMessage = "Please enter both User ID and Password.";
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // ✅ 1. Get stored encrypted password first
                string sql = @"SELECT UserPass 
                       FROM PB_Users 
                       WHERE UserEmail = @UserID AND Active = 'Y'";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@UserID", model.UserID);

                con.Open();
                string storedEncryptedPass = Convert.ToString(cmd.ExecuteScalar());
                con.Close();

                if (string.IsNullOrEmpty(storedEncryptedPass))
                {
                    model.ErrorMessage = "Unauthorized, please contact your admin.";
                    return View(model);
                }


                // ✅ 2. Encrypt entered password for comparison
                string enteredEncryptedPass = util.Encrypt(model.Password);

                // ✅ 3. Validate encrypted password
                sql = @"SELECT COUNT(*) 
                FROM PB_Users 
                WHERE UserEmail = @UserID 
                  AND UserPass = @Pass 
                  AND Active = 'Y'";

                cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@UserID", model.UserID);
                cmd.Parameters.AddWithValue("@Pass", enteredEncryptedPass);

                con.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                con.Close();

                if (count < 1)
                {
                    model.ErrorMessage = "Entered password is wrong";
                    return View(model);
                }

                // ✅ 4. Login success
                Session["UserID"] = model.UserID;


                // ✅ 5. Is default password?
                if (util.IsDefaultPass(model.UserID))
                {
                    return RedirectToAction("ResetPassword", "Login");
                }


                // ✅ 6. Check Admin user
                string IsAdminQuery =
                    $"SELECT CASE WHEN COUNT(*) = 1 THEN 'Y' ELSE 'N' END FROM PB_Users WHERE UserEmail = '{model.UserID}' AND Plant = 'Admin'";
        
        string IsAdmin = util.OpenDataTable(connectionString, IsAdminQuery)
                             .Rows[0][0].ToString();

                if (IsAdmin == "Y")
                {
                    return RedirectToAction("Index", "Admin");
                }


                // ✅ 7. Normal user
                return RedirectToAction("EmbedReport", "Home");
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }

        public ActionResult ResetPassword()
        {
            return View(new ResetPasswordModel());
        }

        [HttpPost]
        public ActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            ResetPasswordModel model = new ResetPasswordModel();
            CommonUtility util = new CommonUtility();

            // ❌ Password mismatch
            if (newPassword != confirmPassword)
            {
                TempData["popupType"] = "error";
                TempData["popupMessage"] = "Passwords do not match!";
                return View(model);
            }

            // ❌ Password validation failed
            if (!util.IsPasswordValid(newPassword))
            {
                TempData["popupType"] = "error";
                TempData["popupMessage"] = "Password should be alphanumeric with special character.";
                return View(model);
            }

            string email = Session["UserID"]?.ToString();

            if (email == null)
                return RedirectToAction("Index");

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand(
                        "UPDATE PB_Users SET UserPass=@P, IsDefault='N', PassRstDt=@PR WHERE UserEmail=@U", con);

                    cmd.Parameters.AddWithValue("@P", util.Encrypt(newPassword));
                    cmd.Parameters.AddWithValue("@U", email);
                    cmd.Parameters.AddWithValue("@PR", DateTime.Now.ToString("yyyyMMdd"));

                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        TempData["popupType"] = "error";
                        TempData["popupMessage"] = "Password reset failed!";
                        return View(model);   // ✅ stay on same page
                    }
                }

                // ✅ Success
                TempData["popupType"] = "success";
                TempData["popupMessage"] = "Password reset successfully!";
                TempData["popupRedirect"] = Url.Action("Index", "Login");

                return RedirectToAction("ResetPassword");   // ✅ reload same view for popup
            }
            catch (Exception ex)
            {
                TempData["popupType"] = "error";
                TempData["popupMessage"] = "Error: " + ex.Message;

                return View(model); // ✅ stay on same view
            }
        }

    }
}

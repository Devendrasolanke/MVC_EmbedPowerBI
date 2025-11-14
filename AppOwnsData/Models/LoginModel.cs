namespace AppOwnsData.Models
{
    public class LoginModel
    {
        public string UserID { get; set; }
        public string Password { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class UserAccessMaster
    {
        public int SrNo { get; set; }
        public string UserEmail { get; set; }
        public int RepID { get; set; }
        public string ReportName { get; set; }
        public string UserPassHash { get; set; }
        public string PassRstDt { get; set; }
        public string Plant { get; set; }
        public string Active { get; set; }
    }

    public class ReportsMaster
    {
        public int RepID { get; set; }
        public string ReportName { get; set; }
        public string ReportID { get; set; }
        public string ReportFilter { get; set; }
    }

    public class ResetPasswordModel
    {
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string ErrorMessage { get; set; }
    }
}
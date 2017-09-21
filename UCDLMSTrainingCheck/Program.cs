using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UCDLMSTrainingCheck.lms.AuthWR;
using UCDLMSTrainingCheck.lms.TransMgntWR;
using UCDLMSTrainingCheck.lms.UserMgntWR;


namespace UCDLMSTrainingCheck
{
    class Program
    {

        static void Main(string[] args)
        {

            //########################################################################
            // Web References for SOAP Calls
            //########################################################################
            //
            // lms.AuthWR
            // https://uc.sumtotal.host/learningapi/services/authentication.asmx
            //
            // lms.UserMgntWR
            // https://uc.sumtotal.host/learningapi/services/usermanagement.asmx
            //
            // lms.TransMgntWR
            // https://uc.sumtotal.host/learningapi/services/transcriptmanagement.asmx
            //
            // After adding references update app.config to use https instead of http
            //
            //########################################################################

            //Var for UCD Mail 
            string ucd_email_addr = "dbunn@ucdavis.edu";

            //Var for LMS ID 
            string lms_id = string.Empty;

            //Var for FERPA Training Taken
            bool bFERPA = false;

            //Var for FERPA Completed Date
            string cmpdFERPA = string.Empty;

            //Var for LMS Error
            string lms_error = string.Empty;

            //Var for LMS Time to Look Back
            DateTime dtLMSLookBack = DateTime.Now.AddYears(-15);

            //Initiate LMS Authentication 
            Authentication auth = new Authentication();
            UserCredentials credentials = new UserCredentials();
            credentials.Username = "YourLMSAccount";
            credentials.Passcode = "YourLMSPassword";
            auth.Login(credentials);


            //// Access the user token sent back to be used later.
            //auth.UserSecurityContextValue = new lms.AuthWR.UserSecurityContext();
            //auth.UserSecurityContextValue.Token = new lms.AuthWR.UserToken();
            //var token = auth.UserTokenValue.Value;
            //auth.UserSecurityContextValue.Token.Value = token;
            //auth.ChangePassword("OldPasswordHere", "NewPassWordHere");


            UserManagement userMgmt = new UserManagement();
            userMgmt.Timeout = 600000; // 10 minutes;
            userMgmt.UserSecurityContextValue = new lms.UserMgntWR.UserSecurityContext();
            userMgmt.UserSecurityContextValue.Token = new lms.UserMgntWR.UserToken();
            userMgmt.UserSecurityContextValue.Token.Value = auth.UserTokenValue.Value;

            TranscriptManagement transcriptMgmt = new TranscriptManagement();
            transcriptMgmt.Timeout = 600000;
            transcriptMgmt.UserSecurityContextValue = new lms.TransMgntWR.UserSecurityContext();
            transcriptMgmt.UserSecurityContextValue.Token = new lms.TransMgntWR.UserToken();
            transcriptMgmt.UserSecurityContextValue.Token.Value = auth.UserTokenValue.Value;

            //Array of LMS Users
            lms.UserMgntWR.User[] lms_users;

            //Pull Users by UCD Email Address
            lms_users = userMgmt.SearchUsersByPath("User[@Email='" + ucd_email_addr + "']", "");

            //Null Empty Check on 
            if (lms_users != null && lms_users.Count() > 0)
            {
                //Loop Through Array Looking for Real Users and Not API Accounts Associated with the Same Email Address
                foreach (lms.UserMgntWR.User lms_accnt in lms_users)
                {

                    //Check for Actual LMS User Account
                    if (lms_accnt.UserStatusId != null && string.IsNullOrEmpty(lms_accnt.Id) == false)
                    {

                        //Console.WriteLine(lms_accnt.FirstName);
                        //Console.WriteLine(lms_accnt.LastName);
                        //Console.WriteLine(lms_accnt.Email);
                        //Console.WriteLine(lms_accnt.Id);
                        //Console.WriteLine(lms_accnt.DomainCode);
                        //Console.WriteLine(lms_accnt.UserStatusId);

                        //Pull LMS ID for Training Transcript Call
                        lms_id = lms_accnt.Id.Replace("User::pk.", "");


                    }//End of Check for Actual LMS User Accounts

                }//End of lms_users foreach

                //Check for LMS ID Before Looking Up Attempts
                if (!string.IsNullOrEmpty(lms_id))
                {

                    //Array of LMS Activity Attempts
                    lms.TransMgntWR.ActivityAttempt[] lms_activity_attempts;

                    try
                    {
                        //Pull Courses from LMS for User
                        lms_activity_attempts = transcriptMgmt.GetUserTranscriptsByDt(dtLMSLookBack.ToUniversalTime(), lms_id, "", 1);

                        //Null\Empty Check on Returned Training Courses
                        if (lms_activity_attempts != null && lms_activity_attempts.Count() > 0)
                        {

                            //Loop Through Training Courses
                            foreach (lms.TransMgntWR.ActivityAttempt lms_atmpt in lms_activity_attempts)
                            {
                                //Only Pull the Completed Courses
                                if (string.IsNullOrEmpty(lms_atmpt.IsComplete.ToString()) == false && lms_atmpt.IsComplete.ToString() == "Completed")
                                {
                                    //Console.WriteLine(lms_atmpt.Id);
                                    //Console.WriteLine(lms_atmpt.ActivityId);
                                    //Console.WriteLine(lms_atmpt.Activity.Name);
                                    //Console.WriteLine(lms_atmpt.EndDate.Value.ToString());
                                    //Console.WriteLine(lms_atmpt.IsComplete.ToString());
                                    //Console.WriteLine(" ");

                                    if (string.IsNullOrEmpty(lms_atmpt.ActivityId) == false && lms_atmpt.ActivityId == "Activity::pk.201699")
                                    {
                                        bFERPA = true;
                                        cmpdFERPA = lms_atmpt.EndDate.Value.ToString();
                                    }

                                }//End of Completed Training Check

                            }//End of Courses Foreach

                        }//End of Returned Courses Null\Empty Check

                    }
                    catch (Exception ex1)
                    {

                        //Check Returned Error Message for No Access to User's Records
                        if (ex1.ToString().Contains("No Viewability on User"))
                        {
                            lms_error = "No Access to this User's training records.";
                        }
                        else
                        {
                            lms_error = ex1.ToString();
                        }

                    }//End of Transcript Try/Catch


                }//End of lms_id Null\Empty Check

            }
            else
            {
                lms_error = "Couldn't pull LMS user account";
            }


            Console.WriteLine("FERPA Completed: " + bFERPA.ToString());
            Console.WriteLine("FERPA Completed On: " + cmpdFERPA);
            Console.WriteLine("LMS Error: " + lms_error);

            EndOfApp();

        }

        static void EndOfApp()
        {
            Console.WriteLine(" ");
            Console.WriteLine("-------------------------------");
            Console.WriteLine("          End of App           ");
            Console.WriteLine("-------------------------------");
            Console.ReadLine();
        }
    }
}

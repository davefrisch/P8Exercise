using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace P8Diagnosis
{
    class Program
    {
        //SQL connection string data that is used later on. Easier to drop here and reference the XML table.
        static string pstrServerName = "";
        public static string pstrSQLConnection = "";
        
        public static void Main()
        {
            //Update public variables to use server in relative file path.
            pstrServerName = File.ReadLines("ServerName.txt").First(); // gets the first line from file.
            pstrSQLConnection = @"Data Source=" + pstrServerName + "; Initial Catalog=Pulse8TestDB;Integrated Security=SSPI";
            Console.WriteLine("Will attempt to use " + pstrServerName + " MS SQL Server Instance.");
            Console.WriteLine("Entire connection string: " + pstrSQLConnection);
            Console.WriteLine();

            //Had some plans to print out the results to a text file, but dropped them later on because I'm not sure how that might work on other machines.
            //Also, easier to call this method from elsewhere and drop it in the main for initial run.
            InitialMenu();
        }
        public static void InitialMenu()
        {
            string strSelection;
            int intmemberId;

            SelectMember://We will come back to this often. This is the start of the process, and selection options. The "main menu".
            Console.Write("Enter MemberId value or enter 'L' for list of available values, or EXIT to exit the program.\nPlease Enter Selection:");
            strSelection = Console.ReadLine();
            if (strSelection.ToUpper() == "L")
            {
                MemberIdSelection();
            }
            else if(strSelection.ToUpper()=="EXIT")
            {
                Environment.Exit(0);
            }
            else if (int.TryParse(strSelection, out intmemberId) == false)
            {
                Console.Clear();
                Console.WriteLine("You entered [" + strSelection + "]. You have not entered an integer value. \nValues entered must be whole numbers only. No alphabetical, special characters, or decimal values are allowed.");
                Console.Write("Would you like to see a list of available MemberId values? (Y/N) :");
                strSelection = Console.ReadLine();
                if (strSelection.ToUpper() == "Y")
                {
                    Console.Clear();
                    MemberIdSelection();
                }
                if (strSelection.ToUpper() == "N")
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Invalid Input. Press enter to return to the main menu.");
                    Console.Read();
                    goto SelectMember;
                }
                goto SelectMember;
            }
            else
            {
                ListOfDiagnosesByMemberId(strSelection);
            }
            strSelection = "";
            Console.WriteLine();
            goto SelectMember;
        }
        public static void MemberIdSelection()
        {
            //This will let the user select how they want the displayed member information sorted, and then display it
            SeeListOfValues:
            Console.Clear();
            Console.WriteLine("Sort values by:\n\t1)MemberId\n\t2)First Name\n\t3)Last Name");
            Console.Write("Please enter selection (1/2/3) or type 'MENU' to return to the initial menu: ");
            string strSelection = Console.ReadLine();
            if (strSelection.ToUpper() == "1")
            {
                Console.WriteLine("Sort by MemberId:");
                ListOfMemberIdValues(strSelection);
            }
            else if (strSelection.ToUpper() == "2")
            {
                Console.WriteLine("Sort by First name:");
                ListOfMemberIdValues(strSelection);
            }
            else if (strSelection.ToUpper() == "3")
            {
                Console.WriteLine("Sort by last name:");
                ListOfMemberIdValues(strSelection);
            }
            else if (strSelection.ToUpper() == "MENU")
            {
                InitialMenu();
            }
            else
            {
                Console.Clear();
                Console.WriteLine("You entered " + strSelection + ". That is not a valid selection.");
                goto SeeListOfValues;
            }
            InitialMenu();
        }

        //Provide the user a list of available member id values to be entered
        public static void ListOfMemberIdValues(string strSortValue)
        {
            Console.Clear();
            string strOrderByList = "";
            //Sort options for displaying values
            if (strSortValue == "1") { strOrderByList = "MemberId, FirstName, LastName;";}
            else if (strSortValue == "2") { strOrderByList = "FirstName, LastName, MemberId;"; }
            else if (strSortValue == "3") { strOrderByList = "LastName, FirstName, MemberId;"; }
            else { strOrderByList = "MemberId, FirstName, LastName;"; }

            string str_SelectDistinctMemberId = "SELECT DISTINCT CAST(MemberId AS VARCHAR(10)) AS MemberId, FirstName, LastName FROM dbo.Member ORDER BY " + strOrderByList;
            using (SqlConnection connection = new SqlConnection(pstrSQLConnection))
            {
                SqlDataAdapter da = new SqlDataAdapter(str_SelectDistinctMemberId, connection);
                DataSet ds = new DataSet();
                da.Fill(ds, "MemberList");

                //Headers to write to console
                Console.WriteLine("| MemberId | FirstName | LastName |");
                foreach (DataRow row in ds.Tables["MemberList"].Rows)
                {
                    //Pring out formatted data to match the header alignment
                    Console.WriteLine(String.Format("|{0,10}|{1,11}|{2,10}|", row["MemberId"].ToString()+" ", row["FirstName"].ToString()+" ", row["LastName"].ToString()+" "));
                }
            }
            Console.WriteLine();
        }

        //Display the results of the query based on the MemberId.
        //Not using a stored procedure because I'm guessing the stored procedure does not exist on the user's machine for evaluation.
        public static void ListOfDiagnosesByMemberId(string strMemberId)
        {
            Console.Clear();
            //The entire query below. Would prefer to use a stored procedure that has this code in production, however.
            string strSQLSyntax = @"
                                        USE Pulse8TestDB

                                        SELECT MemberID, FirstName, LastName, MostSevereDiagnosisId, MostSevereDiagnosisDescription, CategoryId, CategoryDescription, CategoryScore, IsMostSevereCategory
                                        FROM(
                                            SELECT      dM.MemberID, dM.FirstName, dM.LastName
                                                        , CASE WHEN RANK()OVER(PARTITION BY dM.MemberId ORDER BY dD.DiagnosisId) = 1 THEN dD.DiagnosisID END AS MostSevereDiagnosisId/*Identity the lowest DiagnosisId*/
                                                        , CASE WHEN RANK()OVER(PARTITION BY dM.MemberId ORDER BY dD.DiagnosisId) = 1 THEN dD.DiagnosisDescription END AS MostSevereDiagnosisDescription/*Identify diagnosis description of lowest DiagnosisId*/
                                                        , dDC.DiagnosisCategoryID AS CategoryId
                                                        , dDC.CategoryDescription
                                                        , dDC.CategoryScore
                                                        , CASE WHEN RANK()OVER(PARTITION BY dM.MemberId ORDER BY lDC.DiagnosisCategoryId) = 1 AND lDC.DiagnosisID IS NOT NULL THEN 1 ELSE 0 END AS IsMostSevereCategory /*Identify most severe category and set to 0 if no DiagnosisCategoryId exists*/
                                                        , ROW_NUMBER()OVER(PARTITION BY dM.MemberId, dDC.DiagnosisCategoryID ORDER BY dD.DiagnosisId) AS SequenceId /*Identify duplicating categories and filter them out in the higher query*/

                                            FROM        dbo.Member dM
                                            LEFT JOIN   dbo.MemberDiagnosis lMD
                                                        ON lMD.MemberID = dM.MemberID
                                            LEFT JOIN   dbo.Diagnosis dD
                                                        ON dD.DiagnosisID = lMD.DiagnosisID
                                            LEFT JOIN   dbo.DiagnosisCategoryMap lDC
                                                        ON lDC.DiagnosisID = lMD.DiagnosisID
                                            LEFT JOIN   dbo.DiagnosisCategory dDC
                                                        ON dDC.DiagnosisCategoryID = lDC.DiagnosisCategoryID
                                        )X
                                        WHERE       SequenceId = 1
                                                    AND MemberId = " + strMemberId + @"
                                        ORDER BY    MemberID, ISNULL(MostSevereDiagnosisId, 99), ISNULL(CategoryId, 99)
            ";

            //Dump data to a table
            DataTable Table = new DataTable("ResultsTable");
            using (SqlConnection connection = new SqlConnection(pstrSQLConnection))
            {
                SqlDataAdapter da = new SqlDataAdapter(strSQLSyntax, connection);
                DataSet ds = new DataSet();
                da.Fill(ds, "MemberDiagnosisList");

                if (ds.Tables["MemberDiagnosisList"].Rows.Count == 0)
                {
                    Console.WriteLine("No MemberId with the value " + strMemberId + " found in data set.");
                    Console.WriteLine("Press enter to view the list of available values.");
                    Console.ReadLine();
                    ListOfMemberIdValues("1");
                }
                else if (ds.Tables["MemberDiagnosisList"].Rows.Count > 0)
                {
                    Console.WriteLine("List of diaganoses for MemberId: " + strMemberId);
                    //Print the headers in the console
                    Console.WriteLine("| MemberId | FirstName | LastName | MostSevereDiagnosisId | MostSevereDiagnosisDescription | CategoryId | CategoryDescription | CategoryScore | IsMostSevereCategory |");
                    foreach (DataRow row in ds.Tables["MemberDiagnosisList"].Rows)
                    {
                        //Being showing the formatted values to line up with the heads.
                        Console.WriteLine(String.Format("|{0,10}|{1,11}|{2,10}|{3,23}|{4,32}|{5,12}|{6,21}|{7,15}|{8,22}|", row["MemberId"].ToString() + " ", row["FirstName"].ToString() + " ", row["LastName"].ToString() + " ", row["MostSevereDiagnosisId"].ToString() + " ", row["MostSevereDiagnosisDescription"].ToString() + " ", row["CategoryId"].ToString() + " ", row["CategoryDescription"].ToString() + " ", row["CategoryScore"].ToString() + " ", row["IsMostSevereCategory"].ToString() + " "));
                    }
                }
            }
        }
    }
}

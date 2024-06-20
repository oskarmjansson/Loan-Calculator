using LoanCalculator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
namespace LoanCalculator.Controllers;
public class LoanController : Controller
{
    //<Sumary>
    //Server side Controller. This controller is responsible for handling the user's requests and returning the appropriate responses.
    //</summary>
    public static List<Loan> listOfLoans = new List<Loan>();

    //Index: Returns the Index view.
    public ActionResult Index()
    {
        return View();
    }
    //LoanCalculation: Returns the LoanCalculation view. GET.
    public ActionResult LoanCalculation()
    {
        return View();
    }
    //LoanCalculation(Loan loan): Receives the Loan object that the user has filled out in the form.
    //If the model state is valid, we insert the loan into the database and then redirect the user to the "Index" view.
    //If the model state is not valid (meaning the form data does not pass the validation rules),
    //we return the same view along with the Loan object so that the user can correct their mistakes.
    [HttpPost]
    public ActionResult LoanCalculation(Loan loan)
    {
        if (ModelState.IsValid)
        {

            if (loan.amount < 0 || loan.payBackTime < 0 || loan.interest < 0)
            {
                return BadRequest("Amount and payback time must be greater than zero. Interest must be greater than or equal to zero.");
            }
            try
            {
                InsertLoanToDatabase(loan);
                return RedirectToAction("DisplayLoans");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        return View(loan);
    }
    //DisplayLoans: Returns the DisplayLoans view.

    public ActionResult DisplayLoans()
    {
        listOfLoans.Clear();
        using (var connection = new SqliteConnection("Data Source=LoansDB.db"))
        {
            connection.Open();

            using (var command = new SqliteCommand("SELECT * FROM Loans", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double amount = Convert.ToDouble(reader["amount"]);
                        double interest = Convert.ToDouble(reader["interest"]);
                        int payBackTime = Convert.ToInt32(reader["payBackTime"]);

                        Loan loan = new Loan
                        {
                            loanType = reader["loanType"].ToString(),
                            amount = amount,
                            payBackTime = payBackTime,
                            interest = interest,
                        };
                        listOfLoans.Add(loan);
                    }
                }
            }
            connection.Close();
        }
        return View(listOfLoans);
    }
    //EditLoan(string loanTypePara): Returns the EditLoan view.

    public ActionResult EditLoan(string loanTypePara)
    {
        foreach (var loan in listOfLoans)
        {
            if (loan.loanType == loanTypePara)
            {
                return View(loan);
            }
        }
        return NotFound();
    }
    [HttpPost]
    //EditLoan(Loan loanTypePara): Receives the Loan object that the user has filled out in the form.
    //If the model state is valid, we update the loan in the database and then redirect the user to the "DisplayLoans" view.
    //If the model state is not valid (meaning the form data does not pass the validation rules), 
    //an error will be posted so the user understand what went wrong and can correct the mistake.

    public ActionResult EditLoan(Loan? loanTypePara)
    {
        if (loanTypePara.amount < 0 || loanTypePara.payBackTime < 0 || loanTypePara.interest < 0)
        {
            return BadRequest("Amount and payback time must be greater than zero. Interest must be greater than or equal to zero.");
        }
        using (var connection = new SqliteConnection("Data Source=loansDB.db"))
        {
            connection.Open();
            using (var command = new SqliteCommand("UPDATE Loans SET amount = @amount, payBackTime = @payBackTime, interest = @interest WHERE loanType = @loanType", connection))
            {
                command.Parameters.AddWithValue("@amount", loanTypePara.amount);
                command.Parameters.AddWithValue("@payBackTime", loanTypePara.payBackTime);
                command.Parameters.AddWithValue("@interest", loanTypePara.interest);
                command.Parameters.AddWithValue("@loanType", loanTypePara.loanType);

                var result = command.ExecuteNonQueryAsync();

                var loan = listOfLoans.FirstOrDefault(t => t.loanType == loanTypePara.loanType);
                if (loan != null)
                {
                    listOfLoans.RemoveAll(t => t.loanType == loanTypePara.loanType);
                    listOfLoans.Add(loanTypePara);
                    listOfLoans = listOfLoans.OrderBy(x => x.amount).ToList();
                }
            }
            connection.Close();

            return RedirectToAction("DisplayLoans");
        }
    }
    //CalculateMonthlyPaymentsStrait(double amount, double interest, int payBackTime): Calculates the monthly payments for a loan.
    //The method takes the amount of the loan, the interest rate, and the payback time as parameters. 
    //It then calculates the monthly payments using the formula for straight-line amortization.
    //The monthly payments are stored in a list and returned to the caller.
    //If the interest rate is less than zero, an ArgumentException is thrown.
    public List<double> CalculateMonthlyPaymentsStrait(double amount, double interest, int payBackTime)
    {
        if (interest < 0)
        {
            throw new ArgumentException("Interest must be greater than zero.");
        }
        double principalPayment = amount / (payBackTime * 12);
        double monthlyInterestRate = (interest / 100) / 12;

        List<double> monthlyPayments = new List<double>();

        for (int month = 1; month <= payBackTime * 12; month++)
        {
            double remainingPrincipal = amount - (principalPayment * (month - 1));
            double monthlyPayment = principalPayment + (remainingPrincipal * monthlyInterestRate);
            monthlyPayments.Add(Math.Round(monthlyPayment, 0));
        }
        return monthlyPayments;
    }
    //Paymentplan(string loanTypePara): Returns the Paymentplan view. 
    //This method retrieves the loan from the database and calculates the monthly payments using the CalculateMonthlyPaymentsStrait method.
    //The monthly payments are then passed to the view. If the loan does not exist, a 404 error is returned.
    //The loanTypePara parameter is used to identify the loan that the user wants to see the payment plan for.
    public ActionResult Paymentplan(string loanTypePara)
    {
        listOfLoans.Clear();
        Loan loan = null;

        using (var connection = new SqliteConnection("Data Source=LoansDB.db"))
        {
            connection.Open();

            using (var command = new SqliteCommand("SELECT * FROM Loans WHERE loanType = @loanTypePara ", connection))
            {
                command.Parameters.AddWithValue("@loanTypePara", loanTypePara);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double amount = Convert.ToDouble(reader["amount"]);
                        double interest = Convert.ToDouble(reader["interest"]);
                        int payBackTime = Convert.ToInt32(reader["payBackTime"]);

                        loan = new Loan
                        {
                            loanType = reader["loanType"].ToString(),
                            amount = amount,
                            payBackTime = payBackTime,
                            interest = interest,
                        };
                        listOfLoans.Add(loan);
                    }
                }
            }
            connection.Close();
        }
        if (loan != null)
        {
            List<double> payments = CalculateMonthlyPaymentsStrait(loan.amount, loan.interest, loan.payBackTime);
            return View(payments);
        }
        else
        {
            return NotFound();
        }
    }
    //InsertLoanToDatabase(Loan LoanPara): Inserts a loan into the database.
    //First, we check if a loan with the same type already exists in the database.
    //If it does, the user will be sent back to the DisplayLoans view with without any changes.
    public ActionResult InsertLoanToDatabase(Loan LoanPara)
    {
        using (var connection = new SqliteConnection("Data Source=loansDB.db"))
        {
            connection.Open();

            var checkCommandText = "SELECT COUNT(*) FROM Loans WHERE loanType = @loanType";
            using (var checkCommand = new SqliteCommand(checkCommandText, connection))
            {
                checkCommand.Parameters.AddWithValue("@loanType", LoanPara.loanType);
                var count = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (count > 0)
                {
                    return Json(new { success = false, message = "A loan with the same type already exists." });
                }
            }
            var commandText = "INSERT INTO Loans (loanType, amount, payBackTime, interest) VALUES(@loanType, @amount, @payBackTime, @interest)";
            using (var transaction = connection.BeginTransaction())
            {
                using (var insertCommand = new SqliteCommand(commandText, connection, transaction))
                {
                    var paramLoanType = insertCommand.Parameters.Add("@loanType", SqliteType.Text);
                    var paramAmount = insertCommand.Parameters.Add("@amount", SqliteType.Real);
                    var paramPayBackTime = insertCommand.Parameters.Add("@payBackTime", SqliteType.Real);
                    var paramInterest = insertCommand.Parameters.Add("@interest", SqliteType.Real);

                    paramLoanType.Value = LoanPara.loanType;
                    paramAmount.Value = LoanPara.amount;
                    paramPayBackTime.Value = LoanPara.payBackTime;
                    paramInterest.Value = LoanPara.interest;

                    insertCommand.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            connection.Close();
        }
        return Json(new { success = true, redirectUrl = Url.Action("DisplayLoans") });
    }
}

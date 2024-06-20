namespace LoanCalculator.Models;
public class Loan
{
    //<Sumary>
    //This class represents a loan object. It contains the following properties:
    //loanType: The type of loan.
    //amount: The amount of the loan.
    //payBackTime: The time it will take to pay back the loan.
    //interest: The interest rate of the loan.
    //PayBack: The monthly payment for the loan, calculated using the CalculateMonthlyPayment method.
    //</summary>
    public string loanType { get; set; }
    public double amount { get; set; }
    public int payBackTime { get; set; }
    public double interest { get; set; }
    public double PayBack { get; set; }
    public Loan() { }

    public Loan(string loanType, double amount, int payBackTime, double interest)
    {
        this.loanType = loanType;
        this.amount = amount;
        this.payBackTime = payBackTime;
        this.interest = interest;
    }
}
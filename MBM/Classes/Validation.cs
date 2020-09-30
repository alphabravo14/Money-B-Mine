using FluentValidation;
using FluentValidation.Results;
using MBM.Classes;
using System;
using System.Text;
using System.Windows;

namespace MBM
{
    /// <summary>
    /// Uses FluentValidation to validate form fields before submitting
    /// </summary>
    public class Validation : AbstractValidator<DailyPrices>
    {
        public Validation() // Validate class members with LINQ
        {
            RuleFor(x => x.Date.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid date")
                .Must(IsValidDate).WithMessage("Please enter a valid date");

            RuleFor(x => x.Exchange.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid exchange")
                .Length(2, 20).WithMessage("Exchange must be between 2-20 characters");

            RuleFor(x => x.Symbol.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid symbol")
                .Length(2, 20).WithMessage("Symbol must be between 2-20 characters");

            RuleFor(x => x.PriceOpen.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid price open")
                .Must(IsFloat).WithMessage("Invalid value entered for price open (must be a number / float)");

            RuleFor(x => x.PriceClose.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid price close")
                .Must(IsFloat).WithMessage("Invalid value entered for price close (must be a number / float)");

            RuleFor(x => x.PriceLow.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid price low")
                .Must(IsFloat).WithMessage("Invalid value entered for price low (must be a number / float)");

            RuleFor(x => x.PriceHigh.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid price high")
                .Must(IsFloat).WithMessage("Invalid value entered for price high (must be a number / float)");

            RuleFor(x => x.StockPriceAdj.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid stock price adj")
                .Must(IsFloat).WithMessage("Invalid value entered for stock price adj (must be a number / float)");

            RuleFor(x => x.Volume.ToString())
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Please enter a valid volume")
                .Must(IsNumber).WithMessage("Invalid value entered for volume (must be a number)");
        }

        protected bool IsValidDate(string date) // Returns true for valid dates
        {
            DateTime dateTime;
            if (DateTime.TryParse(date, out dateTime))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsFloat(string number) // Returns true for valid floats
        {
            float @float;
            if (float.TryParse(number, out @float))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsNumber(string number) // Returns true for valid numbers
        {
            int @int;
            if (int.TryParse(number, out @int))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Validate the record details before processing / returning error message
        /// </summary>
        internal static bool ValidateRecord(DailyPrices dailyPrices)
        {
            var validation = new Validation();
            var results = validation.Validate(dailyPrices);

            if (!results.IsValid) // Validation failed
            {
                StringBuilder stringBuilder = new StringBuilder("The following vaidation issues occured:\n\n");
                foreach (ValidationFailure error in results.Errors)
                {
                    stringBuilder.AppendLine($"- {error.ErrorMessage}");
                }
                MessageBox.Show(stringBuilder.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return results.IsValid;
        }
    }
}

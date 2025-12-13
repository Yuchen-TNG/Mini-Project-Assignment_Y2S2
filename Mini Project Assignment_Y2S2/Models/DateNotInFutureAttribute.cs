using System;
using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class DateNotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            DateTime inputDate = (DateTime)value;

            if (inputDate.Date > DateTime.Today)
            {
                return new ValidationResult("Date cannot be later than today");
            }

            return ValidationResult.Success;
        }
    }
}

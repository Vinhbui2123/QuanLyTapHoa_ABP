using Abp.Runtime.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Reports.Dto
{
    public class GetRevenueReportInput : ICustomValidate
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string GroupBy { get; set; } = "Day"; // "Day" or "Month"

        public void AddValidationErrors(CustomValidationContext context)
        {
            if (StartDate > EndDate)
            {
                context.Results.Add(new ValidationResult("Ngày bắt đầu không thể lớn hơn ngày kết thúc."));
            }
            if ((EndDate - StartDate).TotalDays > 366)
            {
                context.Results.Add(new ValidationResult("Khoảng thời gian báo cáo không được vượt quá 1 năm (366 ngày)."));
            }
        }
    }
}

using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Helpers
{
  public static  class Extensions
    {
        public static string ToBase64(this string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }
        public static string FromBase64(this string base64Text)
        {
            var base64TextBytes = Convert.FromBase64String(base64Text);
            return Encoding.UTF8.GetString(base64TextBytes);
        }

    }
}

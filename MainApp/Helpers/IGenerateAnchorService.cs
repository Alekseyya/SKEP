using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.Helpers
{
   public interface IGenerateAnchorService
   {
       string GenerateAnchor(string linkText, string action, object routeValues = null);
   }
}

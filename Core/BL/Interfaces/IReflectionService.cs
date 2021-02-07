using System;
using System.Collections.Generic;
using System.Text;

namespace Core.BL.Interfaces
{
    public interface IReflectionService
    {
        /// <summary>
        /// Получает все свойства в объекте. Без IEnumerable<class>. Заменяет поля заканчивающиеся на ID на FullName, Title, ShortName
        /// </summary>
        /// <typeparam name="T">Объект</typeparam>
        /// <param name="entry"></param>
        /// <returns></returns>
        List<(string field, object value)> GetFieldValuesFromObjectThroughProperties<T>(T entry);
    }
}

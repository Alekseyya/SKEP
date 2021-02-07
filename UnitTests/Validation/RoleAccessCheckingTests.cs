using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Models.RBAC;
using Core.Web;
using Microsoft.AspNetCore.Mvc;
using Mono.Cecil;
using Xunit.Abstractions;


namespace RMX.RPCS.UnitTests.Validation
{
    //Реализовать Unit-тест, который генерирует список доступных методов всех контроллеров(Controller, ApiController),которые могут
    //быть вызваны через браузер(public методы с результатами: ActionResult, FileContentResult,
    //string с результатом return JsonConvert.SerializeObject(...)) для каждого класса роли, наследованного от RPCSWebApp.RBAC.Role.

    public class RoleAccessCheckingTests
    {
        private readonly ITestOutputHelper _output;
        public RoleAccessCheckingTests(ITestOutputHelper output)
        {
            _output = output;
        }
        //[Fact] 
        //public void Test_MVCRoleAccessChecking_AvailableMethodsForRole()
        //{
        //    //список методов всех контроллеров
        //    //получаем все классы в нейспейсе унаследованные от класса Controller
        //    var listControllers = AppDomain.CurrentDomain.GetAssemblies()
        //        .SelectMany(t => t.GetTypes())
        //        .Where(t => t.IsClass && t.Namespace == "RMX.RPCS.MainApp.Controllers" && t.IsSubclassOf(typeof(Controller)))
        //        .ToList();

        //    Console.WriteLine("Role;Controller;Method;Access");
        //    //пройтись по всем ролям
        //    foreach (var roleName in GetStringAllRoles())
        //    {
        //        //Console.WriteLine("Роль: " + roleName);
                
        //        //по всем контроллерам
        //        foreach (var currentController in listControllers)
        //        {
        //            //Console.WriteLine("\t Контроллер: " + currentController.Name);
        //            bool methodHaveAttribute = false;

        //            var methodsController = GetAllPublicMethodsController(currentController);

        //            //по всем методам
        //            foreach (var method in methodsController)
        //            {
        //                var customAttribute = GetCustomAttributeOnMethod(method);

        //                //Если есть атрибут у метода, кроме стандартных, только на тех методах на которых есть атрибуты
        //                if (!string.IsNullOrEmpty(customAttribute))
        //                {
        //                    //получить оперейшн из атрибута
        //                    var listOperations = GetOperationInAttribute(customAttribute);
                            
        //                    //если нету у роли оперейшена(т.е не может зайти в этот экшен)
        //                    if (RoleHasAccessToMethod(roleName, listOperations))
        //                    {
        //                        methodHaveAttribute = true;
        //                        Console.WriteLine(roleName + ";" + currentController.Name + ";" + method.Name + ";" + "Open");
        //                    }
        //                    else
        //                    {
        //                        methodHaveAttribute = true;
        //                        Console.WriteLine(roleName + ";" + currentController.Name + ";" + method.Name + ";" + "Close");
        //                    }
        //                }
        //            }
        //            //если у контролеера вообще нету методов с атрибутами
        //            if(!methodHaveAttribute)
        //                //Console.WriteLine("\t" + " - нету методов с атрибутами");
        //                Console.WriteLine("" + ";" + "" + ";" + "" + ";" + "There are no methods with attributes");

        //        }
        //    }
        //}
        //private bool RoleHasAccessToMethod(string roleName, List<string> listOperations)
        //{
        //    var countConditions = 0;
        //    foreach (var operation in listOperations)
        //    {
        //        if (GetOperationsInRole(roleName).Contains(operation))
        //            countConditions++;
        //    }

        //    if (listOperations.Count == countConditions)
        //        return true;
        //    return false;
        //}

        private List<MethodInfo> GetAllPublicMethodsController(Type controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            return Type.GetType(controller.AssemblyQualifiedName)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(par =>
                    par.ReturnType == typeof(ActionResult) || par.ReturnType == typeof(FileContentResult) ||
                    par.ReturnType == typeof(string))
                .Where(m => !typeof(object).GetMethods().Select(me => me.Name)
                    .Contains(m.Name))
                .ToList();
        }

        private string GetCustomAttributeOnMethod(MethodInfo method)
        {
            return method.GetCustomAttributes(true)
                .Where(atr => atr.GetType() != typeof(HttpGetAttribute) &&
                              atr.GetType() != typeof(HttpPostAttribute) &&
                              atr.GetType() != typeof(HttpPutAttribute) &&
                              atr.GetType() != typeof(HttpDeleteAttribute) &&
                              atr.GetType() != typeof(ValidateAntiForgeryTokenAttribute) &&
                              atr.GetType() != typeof(ActionNameAttribute) &&
                              atr.GetType() != typeof(TransactionalActionMvcAttribute) &&
                              atr.GetType() != typeof(NonActionAttribute))
                .Select(n => n.GetType().Name).FirstOrDefault();
        }

        private List<string> GetStringAllRoles()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "RPCSWebApp.RBAC" && t.IsSubclassOf(typeof(Role)))
                .Select(n => n.Name).ToList();
        }

        private List<string> GetOperationsInRole(string className)
        {
            var path = Assembly.GetAssembly(typeof(Role)).Location;
            var assembly = AssemblyDefinition.ReadAssembly(path);

            var roleAdminCtor = assembly.MainModule.GetTypes().Where(c => c.IsClass && c.Name == className)
                .Select(c => c.Methods.FirstOrDefault()).Single();

            var hashSetOperationsString = new HashSet<string>();

            foreach (var instruction in roleAdminCtor.Body.Instructions)
            {
                if (instruction.OpCode.ToString() == "callvirt" && instruction.Operand.ToString().Contains("SetFullMask"))
                    hashSetOperationsString.UnionWith(GetAllOperationsName());
                if (instruction.OpCode.ToString() == "callvirt" && instruction.Operand.ToString().Contains("SetBasicReadonlyOperations"))
                    hashSetOperationsString.UnionWith(GetListOperationInSetBasicOperations());
                if (instruction.OpCode.ToString() == "ldsfld")
                {
                    var operationString = instruction.Operand.ToString().Replace("::", ":").Split(':').Last();
                    hashSetOperationsString.Add(operationString);
                    //if (!hashSetOperationsString.Add(operationString))
                    //    throw  new Exception("Повторяющая операция: " + "Класс: " /*+ classRole*/ + " - " + operationString);
                }
                //Console.WriteLine(instruction.OpCode + "  " + instruction.Operand);
            }
            return hashSetOperationsString.ToList();
        }

        private List<string> GetAllOperationsName()
        {
            return typeof(Operation).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Operation)).Select(n => n.Name).ToList();
        }
       
        private List<string> GetListOperationInSetBasicOperations()
        {
            var pathLocation = Assembly.GetAssembly(typeof(OperationSet)).Location;
            var assembly = AssemblyDefinition.ReadAssembly(pathLocation);

            var setBasicReadonlyOperationsMethod = assembly.MainModule.GetTypes()
                .Where(cl => cl.IsClass && cl.Name == nameof(OperationSet))
                .Select(m => m.Methods.FirstOrDefault(n => n.Name == "SetBasicReadonlyOperations")).Single();
           
            var listStringOperations = new List<string>();

            foreach (var instruction in setBasicReadonlyOperationsMethod.Body.Instructions)
            {
                if (instruction.OpCode.ToString() == "ldsfld")
                {
                    var operationString = instruction.Operand.ToString().Replace("::", ":").Split(':').Last();
                    listStringOperations.Add(operationString);
                }
            }
            return listStringOperations;
        }

        
        //private List<string> GetOperationInAttribute(string className)
        //{
        //    var path = Assembly.GetAssembly(typeof(ADepartmentView)).Location; //TODO наверное надо переделать
        //    var assembly = AssemblyDefinition.ReadAssembly(path);

        //    var onActionExecutingMethod = assembly.MainModule.GetTypes()
        //        .Where(c => c.IsClass && c.Name == className)
        //        .Select(c => c.Methods.FirstOrDefault(x => x.Name == "OnActionExecuting")).Single();

        //    var operationsName = new List<string>();

        //    foreach (var instruction in onActionExecutingMethod.Body.Instructions)
        //    {
        //        if (instruction.OpCode.ToString() == "ldsfld")
        //            operationsName.Add(instruction.Operand.ToString().Replace("::", ":").Split(':').Last());
        //    }
        //    return operationsName;
        //}

        //[Fact]
        //public void Test_GetInharedClassOfIActionFilter()
        //{
        //    var assembly = typeof(AADSyncAccess).GetTypeInfo().Assembly;
        //    var typeList = assembly.GetTypes().Where(t => t.IsClass && t.Namespace == "RMX.RPCS.MainApp.RBAC.Attributes").Select(n => n.Name).ToList();
            
        //    var listRegisterScoped = typeList.Select(l => "services.AddScoped <" + l + "> ();");
        //    var listRegistreFilters = typeList.Select(l => "config.Filters.Add(typeof(" + l + "));");
        //    _output.WriteLine("----------------------");
        //    foreach (var registerScope in listRegisterScoped)
        //    {
        //        _output.WriteLine(registerScope);
        //    }
        //    _output.WriteLine("-----------------");
        //    foreach (var registerFilter in listRegistreFilters)
        //    {
        //        _output.WriteLine(registerFilter);
        //    }
        //}

    }
}


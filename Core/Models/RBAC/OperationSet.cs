using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Models.RBAC
{
    public class OperationSet
    {

        public List<Operation> Operations = new List<Operation>();

        public OperationSet()
        {
        }

        public OperationSet(Operation value)
        {
            if (!Operations.Contains(value))
                Operations.Add(value);
        }

        public static implicit operator OperationSet(Operation operation)
        {
            return new OperationSet(operation);
        }

        public static OperationSet operator |(OperationSet value1, OperationSet value2)
        {
            if (value1 == null)
                value1 = new OperationSet();
            if (value2?.Operations != null)
            {
                value1.Operations.AddRange(value2.Operations);
                var all = value1.Operations.Distinct().ToList();
                value1.Operations = all;
            }
            return value1;
        }

        public void Add(Operation value)
        {
            if (!Operations.Contains(value))
                Operations.Add(value);
        }

        public void SetFullMask()
        {
            for (int operationValue = Operation.MinValue; operationValue < Operation.MaxValue; operationValue++)
            {
                if (!Operations.Contains(new Operation(operationValue)))
                    Operations.Add(new Operation(operationValue));
            }
        }

        public void SetBasicReadonlyOperations()
        {
            Add(Operation.EmployeeView);
            Add(Operation.OrgChartView);
            //Add(Operation.EmployeeSelfUpdate);
            //Add(Operation.ProjectListView);
        }

        public bool Contains(Operation value)
        {
            if (Operations.Contains(value))
                return true;
            return false;
        }
        public void SetPayrollAccessOperations()
        {
            Add(Operation.OOAccessAllow);
            Add(Operation.OOAccessSubEmplReadPayrollAccess);
            Add(Operation.OOAccessFullPayrollAccess);
            Add(Operation.OOAccessFullReadPayrollAccess);
            Add(Operation.OOAccessSubEmplPayrollChangeAccess);
            Add(Operation.OOAccessFullPayrollChangeAccess);
            Add(Operation.OOAccessFullReadPayrollChangeAccess);

            Add(Operation.EmployeePayrollReportView);
        }


        public bool Contains(OperationSet valueSet)
        {
            foreach (var value in valueSet.Operations)
            {
                if (Operations.Contains(value))
                    return true;
            }
            return false;

        }
    }
}

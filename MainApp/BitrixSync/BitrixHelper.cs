using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Core.Config;
using Core.Extensions;
using Core.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;




namespace MainApp.BitrixSync
{
    public class BitrixHelper
    {
        private readonly BitrixConfig _bitrixConfig;

        private string bitrixPortalUrl = "";
        private string bitrixWebHookUrl = "";

        private string bitrixProjectsListID = "";
        private string bitrixProjectStatusListID = "";
        private string bitrixProjectTypeListID = "";

        private bool syncBitrixProjectsEnabled = false;
        private bool syncBitrixProjectRolesEnabled = false;
        private bool syncBitrixDepartmentsEnabled = false;

        private string bitrixAFPListIDs = "";
        private bool syncBitrixExpensesRecordEnabled = false;

        private string bitrixDepartmentListID = "";
        private string bitrixFRCListID = "";
        private string bitrixCSIListID = "";
        private string bitrixRFTListID = "";

        private string bitrixReqEmployeeEnrollmentListID = "";
        private string bitrixProjectRoleListID = "";

        private string bitrixReqPayrollChangeListID = "";

        private string bitrixOrganisationListID = "";
        private bool syncBitrixEmployeeGradEnabled = false;

        public BitrixHelper(IOptions<BitrixConfig> bitrixConfigOptions)
        {
            _bitrixConfig = bitrixConfigOptions.Value;

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncPortalUrl))
            {
                bitrixPortalUrl = _bitrixConfig.SyncPortalUrl;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncWebHookUrl))
            {
                bitrixWebHookUrl = _bitrixConfig.SyncWebHookUrl;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncProjectsListID))
            {
                bitrixProjectsListID = _bitrixConfig.SyncProjectsListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncProjectStatusListID))
            {
                bitrixProjectStatusListID = _bitrixConfig.SyncProjectStatusListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncProjectTypeListID))
            {
                bitrixProjectTypeListID = _bitrixConfig.SyncProjectTypeListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncProjectsEnabled))
            {
                try
                {
                    syncBitrixProjectsEnabled = Boolean.Parse(_bitrixConfig.SyncProjectsEnabled);
                }
                catch (Exception)
                {
                    syncBitrixProjectsEnabled = false;
                }
            }
            if (_bitrixConfig.SyncProjectRolesEnabled)
            {
                try
                {
                    syncBitrixProjectRolesEnabled = _bitrixConfig.SyncProjectRolesEnabled;
                }
                catch (Exception)
                {
                    syncBitrixProjectRolesEnabled = false;
                }
            }

            try
            {
                syncBitrixDepartmentsEnabled = _bitrixConfig.SyncDepartmentsEnabled;
            }
            catch (Exception)
            {
                syncBitrixDepartmentsEnabled = false;
            }



            if (!string.IsNullOrEmpty(_bitrixConfig.SyncAfpListIDs))
            {
                bitrixAFPListIDs = _bitrixConfig.SyncAfpListIDs;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncExpensesRecordEnabled))
            {
                try
                {
                    syncBitrixExpensesRecordEnabled = Boolean.Parse(_bitrixConfig.SyncExpensesRecordEnabled);
                }
                catch (Exception)
                {
                    syncBitrixExpensesRecordEnabled = false;
                }
            }

            if (_bitrixConfig.SyncDepartmentListID != null)
            {
                bitrixDepartmentListID = _bitrixConfig.SyncDepartmentListID.ToString();
            }


            if (!string.IsNullOrEmpty(_bitrixConfig.SyncFRCListID))
            {
                bitrixFRCListID = _bitrixConfig.SyncFRCListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncCSIListID))
            {
                bitrixCSIListID = _bitrixConfig.SyncCSIListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncRFTListID))
            {
                bitrixRFTListID = _bitrixConfig.SyncRFTListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncEmployeeGradEnabled))
            {
                try
                {
                    syncBitrixEmployeeGradEnabled = Boolean.Parse(_bitrixConfig.SyncEmployeeGradEnabled);
                }
                catch (Exception)
                {
                    syncBitrixEmployeeGradEnabled = false;
                }
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncReqEmployeeEnrollmentListID))
            {
                bitrixReqEmployeeEnrollmentListID = _bitrixConfig.SyncReqEmployeeEnrollmentListID;
            }

            if (!string.IsNullOrEmpty(_bitrixConfig.SyncReqPayrollChangeListID))
            {
                bitrixReqPayrollChangeListID = _bitrixConfig.SyncReqPayrollChangeListID;
            }
            if (_bitrixConfig.SyncOrganisationListID != null)
            {
                bitrixOrganisationListID = _bitrixConfig.SyncOrganisationListID.ToString();
            }
        }

        public string SendCommand(string Command, string Params = "", string Body = "")
        {
            string bitrixRestUri = "";

            bitrixRestUri = bitrixWebHookUrl + "/" + Command + "?" + Params.Replace(" ", "%20");

            HttpWebRequest requestBitrixRest = (HttpWebRequest)WebRequest.Create(bitrixRestUri);
            requestBitrixRest.Method = "POST";

            byte[] byteArrayBody = Encoding.UTF8.GetBytes(Body);
            requestBitrixRest.ContentType = "application/x-www-form-urlencoded";
            requestBitrixRest.ContentLength = byteArrayBody.Length;
            requestBitrixRest.ServerCertificateValidationCallback = delegate { return true; };
            Stream dataBodyStream = requestBitrixRest.GetRequestStream();
            dataBodyStream.Write(byteArrayBody, 0, byteArrayBody.Length);
            dataBodyStream.Close();

            HttpWebResponse responseBitrixRest = (HttpWebResponse)requestBitrixRest.GetResponse();

            Stream dataStreamBitrixRest = responseBitrixRest.GetResponseStream();
            var readerBitrixRest = new StreamReader(dataStreamBitrixRest);
            string stringBitrixRest = readerBitrixRest.ReadToEnd();

            //stringBitrixRest = System.Text.RegularExpressions.Regex.Unescape(stringBitrixRest);

            readerBitrixRest.Close();
            dataStreamBitrixRest.Close();
            responseBitrixRest.Close();
            return stringBitrixRest;
        }

        private IList GetBitrixList<T>(string listId, Dictionary<string, string> elementParams = null)
        {
            List<T> resultList = null;
            if (String.IsNullOrEmpty(listId))
                return resultList;


            string startIndex = null;

            string listPropertyMapping = GetBitrixListPropertyMapping(listId);
            var elementConditions = elementParams == null ? "" : "&" + String.Join("&", elementParams.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            do
            {
                string answer = SendCommand("lists.element.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listId
                    + ((String.IsNullOrEmpty(startIndex) == true) ? "" : ("&start=" + startIndex)) + elementConditions, "");

                string jsonDataArrayString = "";
                if (String.IsNullOrEmpty(answer) == false)
                {
                    Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                    if (resultData != null && resultData.ContainsKey("result") == true)
                    {
                        jsonDataArrayString = resultData["result"].ToString();
                    }

                    if (resultData != null && resultData.ContainsKey("next") == true)
                    {
                        startIndex = resultData["next"].ToString();
                    }
                    else
                    {
                        startIndex = null;
                    }
                }

                if (String.IsNullOrEmpty(jsonDataArrayString) == false)
                {

                    if (String.IsNullOrEmpty(listPropertyMapping) == false)
                    {
                        string[] pairs = listPropertyMapping.Split(',');
                        foreach (string pair in pairs)
                        {
                            jsonDataArrayString = jsonDataArrayString.Replace(pair.Split(':')[0], pair.Split(':')[1]);
                        }
                    }

                    if (resultList == null)
                        resultList = JsonConvert.DeserializeObject<List<T>>(jsonDataArrayString);
                    else
                        resultList.AddRange(JsonConvert.DeserializeObject<List<T>>(jsonDataArrayString));
                }

            }
            while (String.IsNullOrEmpty(startIndex) == false);


            return resultList;

        }


        public void UpdateBitrixListElement(BitrixListElement bitrixListElement,
            string propertyName, string propertyValue)
        {
            Dictionary<string, BitrixListField> fields = GetListProperties(bitrixListElement.IBLOCK_ID);
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixListElement.IBLOCK_ID);

            string answer = SendCommand("lists.element.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixListElement.IBLOCK_ID
                + "&ELEMENT_ID=" + bitrixListElement.ID);

            Dictionary<string, object> originalElement = null;

            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                if (resultData != null
                    && resultData.ContainsKey("result") == true
                    && resultData["result"] != null
                    && String.IsNullOrEmpty(resultData["result"].ToString()) == false)
                {
                    List<Dictionary<string, object>> elementList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultData["result"].ToString());

                    if (elementList != null
                        && elementList.Count == 1)
                    {
                        originalElement = elementList[0];
                    }
                }
            }

            if (originalElement != null)
            {
                Dictionary<string, object> originalValues = new Dictionary<string, object>();
                foreach (string key in originalElement.Keys)
                {
                    object value = originalElement[key];
                    string strValue = null;
                    string[] strArrayValue = null;

                    if (value != null)
                    {
                        if (typeof(string).IsInstanceOfType(value) == true)
                        {
                            strValue = value.ToString();
                        }
                        else
                        {
                            Dictionary<string, string> valDictionary = null;
                            Dictionary<string, string[]> valDictionaryArray = null;

                            try
                            {
                                valDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(value.ToString());
                            }
                            catch (Exception)
                            {

                            }

                            if (valDictionary != null
                                && valDictionary.Count == 1
                                && valDictionary.FirstOrDefault().Value != null)
                            {
                                strValue = valDictionary.FirstOrDefault().Value;
                            }
                            else if (valDictionary != null
                                && valDictionary.Count > 1)
                            {
                                foreach (string k in valDictionary.Keys)
                                {
                                    if (String.IsNullOrEmpty(strValue) == true)
                                    {
                                        strValue = valDictionary[k];
                                    }
                                    else
                                    {
                                        strValue += "," + valDictionary[k];
                                    }
                                }
                            }

                            if (valDictionary == null)
                            {
                                try
                                {
                                    valDictionaryArray = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(value.ToString());
                                }
                                catch (Exception)
                                {

                                }

                                if (valDictionaryArray != null
                                    && valDictionaryArray.Count == 1
                                    && valDictionaryArray.FirstOrDefault().Value != null)
                                {
                                    strArrayValue = valDictionaryArray.FirstOrDefault().Value;
                                }
                            }
                        }

                        if (String.IsNullOrEmpty(strValue) == false)
                        {
                            if (fields.ContainsKey(key) == true
                                && fields[key] != null
                                && fields[key].TYPE != null
                                && fields[key].TYPE.Equals("S:Money") == true)
                            {
                                if (strValue.EndsWith("руб.") == true)
                                {
                                    strValue = strValue.Replace("руб.", "|RUB").Replace(" ", "").Replace(".00", "").Trim();
                                }

                                if (strValue.Contains("|RUB") == true)
                                {
                                    double doubleValue = Convert.ToDouble(strValue.Replace("|RUB", "").Replace(".", ","));
                                    if (doubleValue >= 0)
                                    {
                                        strValue = (doubleValue.ToString("0.00") + "|RUB").Replace(",", ".").Replace(".00", "");
                                    }
                                    else
                                    {
                                        strValue = "0|RUB";
                                    }
                                }
                                else if (strValue.Contains("|USD") == true)
                                {
                                    double doubleValue = Convert.ToDouble(strValue.Replace("|USD", "").Replace(".", ","));
                                    if (doubleValue >= 0)
                                    {
                                        strValue = (doubleValue.ToString("0.00") + "|USD").Replace(",", ".").Replace(".00", "");
                                    }
                                    else
                                    {
                                        strValue = "0|USD";
                                    }
                                }
                            }

                            if (strValue.Equals("0|RUB") == false
                                && strValue.Equals("0|USD") == false)
                            {
                                originalValues.Add(key, strValue);
                            }

                        }


                        if (strArrayValue != null)
                        {
                            originalValues.Add(key, strArrayValue);
                        }
                    }
                }

                StringBuilder sbParams = new StringBuilder();
                //sbParams.AppendFormat("&{0}={1}", "ELEMENT_CODE", "Test");
                //sbParams.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", originalValues["ELEMENT_CODE"]);
                sbParams.AppendFormat("&FIELDS[{0}]={1}", "NAME", originalValues["NAME"]);
                //sbParams.AppendFormat("&FIELDS[{0}]={1}", "CODE", originalValues["CODE"]);

                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName(propertyName, listPropertyMapping), propertyValue);
                originalValues.Remove(GetBitrixPropertyName(propertyName, listPropertyMapping));

                foreach (string key in originalValues.Keys)
                {

                    if (key.StartsWith("PROPERTY_") == true)
                    {
                        if (typeof(string[]).IsInstanceOfType(originalValues[key]) == true)
                        {
                            string[] strArray = originalValues[key] as string[];
                            foreach (string str in strArray)
                            {
                                sbParams.AppendFormat("&FIELDS[{0}][]={1}", key,
                                    HttpUtility.JavaScriptStringEncode(str));
                            }
                        }
                        else
                        {
                            sbParams.AppendFormat("&FIELDS[{0}]={1}", key,
                                HttpUtility.UrlEncode(originalValues[key].ToString().Replace("+", "%20")));
                        }
                    }
                }


                string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixListElement.IBLOCK_ID + "&ELEMENT_ID=" + bitrixListElement.ID + sbParams.ToString());
            }

        }

        public List<BitrixProject> GetBitrixProjectList()
        {
            return GetBitrixList<BitrixProject>(bitrixProjectsListID) as List<BitrixProject>;
        }

        //BitrixProjectStatusList

        public List<BitrixProjectStatus> GetBitrixProjectStatusList()
        {
            return GetBitrixList<BitrixProjectStatus>(bitrixProjectStatusListID) as List<BitrixProjectStatus>;
        }

        public BitrixProjectStatus GetBitrixProjectStatusByValue(string value)
        {
            return GetBitrixProjectStatusList().FirstOrDefault(x => x.Value.Values.First() == value);
        }

        public BitrixProjectStatus GetBitrixProjectStatusById(string id)
        {
            return GetBitrixProjectStatusList().FirstOrDefault(x => x.ID == id);
        }

        //BitrixProjectTypeList

        public List<BitrixProjectType> GetBitrixProjectTypeList()
        {
            return GetBitrixList<BitrixProjectType>(bitrixProjectTypeListID) as List<BitrixProjectType>;
        }

        public BitrixProjectType GetBitrixProjectTypeByShortName(string shortName)
        {
            return GetBitrixProjectTypeList().FirstOrDefault(x => x.ShortName.Values.First() == shortName);
        }

        public BitrixProjectType GetBitrixProjectTypeById(string id)
        {
            return GetBitrixProjectTypeList().FirstOrDefault(x => x.ID == id);
        }

        public bool IsSyncBitrixProjectsEnabled()
        {
            return syncBitrixProjectsEnabled;
        }

        public bool IsSyncBitrixProjectRolesEnabled()
        {
            return syncBitrixProjectRolesEnabled;
        }
        public bool IsSyncBitrixDepartmentsEnabled()
        {
            return syncBitrixDepartmentsEnabled;
        }

        //BitrixFRCList

        public List<BitrixFRC> GetBitrixFRCList()
        {
            return GetBitrixList<BitrixFRC>(bitrixFRCListID) as List<BitrixFRC>;
        }

        public List<BitrixCSI> GetBitrixCSIList()
        {
            return GetBitrixList<BitrixCSI>(bitrixCSIListID) as List<BitrixCSI>;
        }

        public BitrixCSI GetBitrixCSIById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixCSI>(bitrixCSIListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixCSI>;
            return result.FirstOrDefault();
        }

        string GetBitrixPropertyName(string mappingProperty, string listPropertyMapping)
        {
            // приведем названия полей к используемым нами
            if (String.IsNullOrEmpty(listPropertyMapping) == false)
            {
                string[] pairs = listPropertyMapping.Split(',');
                foreach (string pair in pairs)
                {
                    if (pair.Split(':')[1].Trim() == mappingProperty.Trim())
                    {
                        return pair.Split(':')[0].Trim();
                    }
                }
            }

            throw new Exception("Не найдено свойство для " + mappingProperty);
            //return mappingProperty;
        }

        public void UpdateBitrixProject(Project project, string elementID)
        {
            if (String.IsNullOrEmpty(bitrixProjectsListID) || String.IsNullOrEmpty(bitrixProjectStatusListID))
                return;

            var sbParams = GetBitrixProjectBaseParams(project);
            if (sbParams != null)
            {
                string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixProjectsListID + "&ELEMENT_ID=" + elementID.ToString() + sbParams.ToString());
            }

        }

        public void AddBitrixProject(Project project)
        {
            if (String.IsNullOrEmpty(bitrixProjectsListID) || String.IsNullOrEmpty(bitrixProjectStatusListID))
                return;
            var sbParams = GetBitrixCreateProjectParams(project);
            if (sbParams != null)
            {
                string createElement = SendCommand("lists.element.add", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixProjectsListID + sbParams.ToString());
            }
        }

        public void UpdateBitrixFRC(Department department, string elementID)
        {
            if (String.IsNullOrEmpty(bitrixFRCListID))
                return;

            var sbParams = GetBitrixFRCBaseParams(department);
            string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixFRCListID + "&ELEMENT_ID=" + elementID.ToString() + sbParams.ToString());
        }

        public void AddBitrixFRC(Department department)
        {
            if (String.IsNullOrEmpty(bitrixFRCListID))
                return;

            var sbParams = GetBitrixCreateFRCParams(department);
            string createElement = SendCommand("lists.element.add", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixFRCListID + sbParams.ToString());

        }

        #region ProjectRole
        public void AddBitrixProjectRole(ProjectRole role)
        {
            if (String.IsNullOrEmpty(bitrixProjectRoleListID))
                return;

            var sbParams = GetBitrixCreateProjectRoleParams(role);
            string createElement = SendCommand("lists.element.add", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixProjectRoleListID + sbParams.ToString());
        }

        public void UpdateBitrixProjectRole(ProjectRole role, string elementID)
        {
            if (String.IsNullOrEmpty(bitrixProjectRoleListID))
                return;

            var sbParams = GetBitrixProjectRoleParams(role);
            string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixProjectRoleListID + "&ELEMENT_ID=" + elementID + sbParams.ToString());
        }

        private StringBuilder GetBitrixCreateProjectRoleParams(ProjectRole role)
        {
            var sb = GetBitrixProjectRoleParams(role);
            sb.AppendFormat("&{0}={1}", "ELEMENT_CODE", role.ShortName.Replace("\"", ""));
            return sb;
        }

        private StringBuilder GetBitrixProjectRoleParams(ProjectRole role)
        {
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixProjectRoleListID);
            var sb = new StringBuilder();

            sb.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", role.ShortName.Replace("\"", ""));
            sb.AppendFormat("&FIELDS[{0}]={1}", "NAME", role.Title.Replace("\"", ""));
            sb.AppendFormat("&FIELDS[{0}]={1}", "CODE", role.Title.Replace("\"", ""));
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SKIPR_ID", listPropertyMapping), role.ID);
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SHORT_NAME", listPropertyMapping), role.ShortName);
            return sb;
        }

        #endregion


        #region Department
        public void AddBitrixDepartment(Department department, string bitrixParentID = null)
        {
            if (String.IsNullOrEmpty(bitrixDepartmentListID))
                return;

            var sbParams = GetBitrixCreateDepartmentParams(department, bitrixParentID);
            string createElement = SendCommand("lists.element.add", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixDepartmentListID + sbParams.ToString());
        }

        private StringBuilder GetBitrixCreateDepartmentParams(Department department, string bitrixParentID = null)
        {
            var sb = GetBitrixDepartmentParams(department, bitrixParentID);
            sb.AppendFormat("&{0}={1}", "ELEMENT_CODE", department.ShortName.Replace("\"", ""));
            return sb;
        }

        private StringBuilder GetBitrixDepartmentParams(Department department, string bitrixParentID = null)
        {
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixDepartmentListID);
            var bitrixDM = GetBitrixUserByEmail(department.DepartmentManager?.Email);
            var sb = new StringBuilder();

            sb.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", department.ShortName.Replace("\"", ""));
            sb.AppendFormat("&FIELDS[{0}]={1}", "NAME", String.Join(". ", department.ShortName, department.Title).Replace("\"", ""));
            // sb.AppendFormat("&FIELDS[{0}]={1}", "CODE", dept.Title.Replace("\"", ""));
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SHORT_TITLE", listPropertyMapping), department.ShortTitle);
            if (bitrixParentID != null) sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("PARENT_DEPARTMENT", listPropertyMapping), bitrixParentID);
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("DEPARTMENT_MANAGER", listPropertyMapping), bitrixDM?.ID);
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("IS_FINANCIAL_CENTRE", listPropertyMapping), department.IsFinancialCentre.BoolToYOrN());
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("IS_AUTONOMOUS", listPropertyMapping), department.IsAutonomous.BoolToYOrN());
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SKIPR_ID", listPropertyMapping), department.ID);
            sb.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SHORT_NAME", listPropertyMapping), department.ShortName);
            return sb;
        }

        public void UpdateBitrixDepartment(Department department, string elementID, string bitrixParentID = null)
        {
            if (String.IsNullOrEmpty(bitrixDepartmentListID))
                return;

            var sbParams = GetBitrixDepartmentParams(department, bitrixParentID);
            string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixDepartmentListID + "&ELEMENT_ID=" + elementID + sbParams.ToString());
        }
        #endregion



        public void UpdateBitrixCSI(CostSubItem costSubItem, BitrixCSI bitrixCSI)
        {
            if (String.IsNullOrEmpty(bitrixCSIListID))
                return;

            var sbParams = GetBitrixCSIBaseParams(costSubItem, bitrixCSI);
            string updateElement = SendCommand("lists.element.update", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixCSIListID + "&ELEMENT_ID=" + bitrixCSI.ID.ToString() + sbParams.ToString());
        }

        public void AddBitrixCSI(CostSubItem costSubItem)
        {
            if (String.IsNullOrEmpty(bitrixCSIListID))
                return;

            var sbParams = GetBitrixCreateCSIParams(costSubItem);
            string createElement = SendCommand("lists.element.add", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + bitrixCSIListID + sbParams.ToString());

        }

        private StringBuilder GetBitrixCreateCSIParams(CostSubItem costSubItem)
        {
            var base_params = GetBitrixCSIBaseParams(costSubItem, null);
            base_params.AppendFormat("&{0}={1}", "ELEMENT_CODE", costSubItem.ShortName.Replace("\"", ""));
            return base_params;
        }

        private StringBuilder GetBitrixCSIBaseParams(CostSubItem costSubItem, BitrixCSI bitrixCSI)
        {
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixCSIListID);
            StringBuilder sbParams = new StringBuilder();
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", costSubItem.ShortName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "NAME", costSubItem.FullName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "CODE", costSubItem.FullName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("CostItemTitle", listPropertyMapping), costSubItem.CostItem?.Title.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("CostSubitemTitle ", listPropertyMapping), costSubItem.Title.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("CostSubitemDescription", listPropertyMapping), costSubItem.Description.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SKIPR_ID", listPropertyMapping), costSubItem.ID);

            if (bitrixCSI != null)
            {
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ReviewerStage1Mode", listPropertyMapping), bitrixCSI.ReviewerStage1Mode?.Values.FirstOrDefault());
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ReviewerStage1User", listPropertyMapping), bitrixCSI.ReviewerStage1User?.Values.FirstOrDefault());
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ReviewerStage2User", listPropertyMapping), bitrixCSI.ReviewerStage2User?.Values.FirstOrDefault());
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("CostType", listPropertyMapping), bitrixCSI.CostType?.Values.FirstOrDefault());
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("TSFO", listPropertyMapping), bitrixCSI.TSFO?.Values.FirstOrDefault());
            }
            return sbParams;
        }

        private StringBuilder GetBitrixCreateFRCParams(Department department)
        {
            var base_params = GetBitrixFRCBaseParams(department);
            base_params.AppendFormat("&{0}={1}", "ELEMENT_CODE", department.ShortName.Replace("\"", ""));
            return base_params;
        }

        private StringBuilder GetBitrixFRCBaseParams(Department department)
        {
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixFRCListID);
            var bitrixDM = GetBitrixUserByEmail(department.DepartmentManager?.Email);
            StringBuilder sbParams = new StringBuilder();

            var departmentName = String.Join(". ", department.ShortName, department.ShortTitle, department.Title);
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", department.ShortName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "NAME", departmentName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", "CODE", departmentName.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("FinResponsibilityCenterItem", listPropertyMapping), department.ShortTitle.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("DepartmentTitle ", listPropertyMapping), department.Title.Replace("\"", ""));
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SKIPR_ID", listPropertyMapping), department.ID);
            sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("FinResponsibilityCenterHead", listPropertyMapping), bitrixDM?.ID);

            return sbParams;
        }


        private StringBuilder GetBitrixCreateProjectParams(Project project)
        {
            StringBuilder sbParams = GetBitrixProjectBaseParams(project);
            if (sbParams != null)
            {
                sbParams.AppendFormat("&{0}={1}", "ELEMENT_CODE", project.ShortName.Replace("\"", ""));
            }
            return sbParams;
        }

        private StringBuilder GetBitrixProjectBaseParams(Project project)
        {
            string listPropertyMapping = GetBitrixListPropertyMapping(bitrixProjectsListID);
            var bitrixPM = GetBitrixUserByEmail(project.EmployeePM?.Email);
            var bitrixCAM = GetBitrixUserByEmail(project.EmployeeCAM?.Email);
            if (bitrixPM != null /*&& bitrixCAM != null*/)
            {
                StringBuilder sbParams = new StringBuilder();

                sbParams.AppendFormat("&FIELDS[{0}]={1}", "ELEMENT_CODE", project.ShortName.Replace("\"", ""));
                sbParams.AppendFormat("&FIELDS[{0}]={1}", "NAME", project.ShortName.Replace("\"", ""));
                sbParams.AppendFormat("&FIELDS[{0}]={1}", "CODE", project.ShortName.Replace("\"", ""));
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ProjectTitle", listPropertyMapping), HttpUtility.UrlPathEncode(project.Title.Replace("\"", "").Replace("&", "_").Replace("+", "_")));
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("SKIPR_ID", listPropertyMapping), project.ID);
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ProjectPM", listPropertyMapping), bitrixPM?.ID);
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ProjectCAM", listPropertyMapping), bitrixCAM?.ID);
                var bitrixFRC = GetBitrixFRCByDepartmentShortName(project.Department?.ShortName);
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("FinResponsibilityCenter", listPropertyMapping), bitrixFRC?.ID);
                var projectStatus = ((int)project.Status).ToString();
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ProjectStatus", listPropertyMapping), GetBitrixProjectStatusByValue(projectStatus)?.ID);
                var bitrixProjectType = GetBitrixProjectTypeByShortName(project.ProjectType?.ShortName);
                sbParams.AppendFormat("&FIELDS[{0}]={1}", GetBitrixPropertyName("ProjectType", listPropertyMapping), bitrixProjectType?.ID);
                return sbParams;
            }
            else
            {
                return null;
            }
        }

        public string GetBitrixListPropertyMapping(string listID)
        {
            string listPropertyMapping = "";

            string answer = SendCommand("lists.field.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listID.ToString());

            string jsonDataArrayString = "";
            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer.Replace("\\", "_"));
                if (resultData != null && resultData.ContainsKey("result") == true)
                {
                    jsonDataArrayString = resultData["result"].ToString();
                }
            }

            if (String.IsNullOrEmpty(jsonDataArrayString) == false)
            {
                Hashtable fields = JsonConvert.DeserializeObject<Hashtable>(jsonDataArrayString);
                if (fields != null)
                {
                    foreach (string fieldName in fields.Keys)
                    {
                        string jsonField = fields[fieldName].ToString();

                        if (String.IsNullOrEmpty(jsonField) == false)
                        {
                            try
                            {
                                BitrixListField field = JsonConvert.DeserializeObject<BitrixListField>(jsonField);

                                if (field != null
                                    && String.IsNullOrEmpty(field.FIELD_ID) == false
                                    && String.IsNullOrEmpty(field.CODE) == false)
                                {
                                    if (String.IsNullOrEmpty(listPropertyMapping) == true)
                                    {
                                        listPropertyMapping = field.FIELD_ID + ":" + field.CODE;
                                    }
                                    else
                                    {
                                        listPropertyMapping += "," + field.FIELD_ID + ":" + field.CODE;
                                    }
                                }
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                }
            }

            return listPropertyMapping;
        }

        protected Dictionary<string, BitrixListField> GetListProperties(string listID)
        {
            Dictionary<string, BitrixListField> fields = null;

            string answer = SendCommand("lists.field.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listID.ToString());

            string jsonDataArrayString = "";
            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer.Replace("\\", "_"));
                if (resultData != null && resultData.ContainsKey("result") == true)
                {
                    jsonDataArrayString = resultData["result"].ToString();
                }
            }

            if (String.IsNullOrEmpty(jsonDataArrayString) == false)
            {
                fields = JsonConvert.DeserializeObject<Dictionary<string, BitrixListField>>(jsonDataArrayString);
            }

            return fields;
        }

        public Dictionary<string, string> GetBitrixListPropertyDisplayValuesForm(string listID, string fieldCode)
        {
            Dictionary<string, string> fieldDisplayValuesForm = null;

            string answer = SendCommand("lists.field.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listID.ToString());

            string jsonDataArrayString = "";
            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer.Replace("\\", "_"));
                if (resultData != null && resultData.ContainsKey("result") == true)
                {
                    jsonDataArrayString = resultData["result"].ToString();
                }
            }

            if (String.IsNullOrEmpty(jsonDataArrayString) == false)
            {
                Hashtable fields = JsonConvert.DeserializeObject<Hashtable>(jsonDataArrayString);
                if (fields != null)
                {
                    foreach (string fieldName in fields.Keys)
                    {
                        string jsonField = fields[fieldName].ToString();

                        if (String.IsNullOrEmpty(jsonField) == false)
                        {
                            try
                            {
                                BitrixListField field = JsonConvert.DeserializeObject<BitrixListField>(jsonField);

                                if (field != null
                                    && String.IsNullOrEmpty(field.CODE) == false
                                    && field.CODE.Equals(fieldCode) == true)
                                {
                                    fieldDisplayValuesForm = field.DISPLAY_VALUES_FORM;
                                    break;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }

            return fieldDisplayValuesForm;
        }

        public List<BitrixApplicationForPayment> GetBitrixAFPList(string listID)
        {
            List<BitrixApplicationForPayment> applicationForPaymentList = new List<BitrixApplicationForPayment>();
            string startIndex = null;

            string listPropertyMapping = GetBitrixListPropertyMapping(listID);

            do
            {
                string answer = SendCommand("lists.element.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listID
                    + ((String.IsNullOrEmpty(startIndex) == true) ? "" : ("&start=" + startIndex))
                    /*+ "&FILTER=" + Uri.EscapeUriString("{ '>=DATE_CREATE': '01.01.2018 00:00:00', '<=DATE_CREATE': '12.04.2018 23:59:59', }")*/, "");

                string jsonDataArrayString = "";
                if (String.IsNullOrEmpty(answer) == false)
                {
                    Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                    if (resultData != null && resultData.ContainsKey("result") == true)
                    {
                        jsonDataArrayString = resultData["result"].ToString();
                    }

                    if (resultData != null && resultData.ContainsKey("next") == true)
                    {
                        startIndex = resultData["next"].ToString();
                    }
                    else
                    {
                        startIndex = null;
                    }
                }

                if (String.IsNullOrEmpty(jsonDataArrayString) == false)
                {
                    if (String.IsNullOrEmpty(listPropertyMapping) == false)
                    {
                        string[] pairs = listPropertyMapping.Split(',');
                        foreach (string pair in pairs)
                        {
                            jsonDataArrayString = jsonDataArrayString.Replace(pair.Split(':')[0], pair.Split(':')[1]);
                        }
                    }

                    applicationForPaymentList.AddRange(JsonConvert.DeserializeObject<List<BitrixApplicationForPayment>>(jsonDataArrayString));
                }

            }
            while (String.IsNullOrEmpty(startIndex) == false);

            return applicationForPaymentList;
        }

        public string GetBitrixAFPListIDs()
        {
            return bitrixAFPListIDs;
        }

        public List<BitrixRequestForTrip> GetBitrixRequestForTrip()
        {
            if (String.IsNullOrEmpty(bitrixRFTListID))
                return null;

            return GetBitrixList<BitrixRequestForTrip>(bitrixRFTListID) as List<BitrixRequestForTrip>;
        }

        public string GetBitrixRFTListID()
        {
            return bitrixRFTListID;
        }

        public bool IsSyncBitrixExpensesRecordEnabled()
        {
            return syncBitrixExpensesRecordEnabled;
        }

        //Employee Grad
        public bool IsSyncBitrixEmployeeGradEnabled()
        {
            return syncBitrixEmployeeGradEnabled;
        }

        public void UpdateBitrixUserEmployeeGrad(BitrixUser bitrixUser, string gradValue)
        {
            if (bitrixUser == null || String.IsNullOrEmpty(gradValue))
                return;

            string updateElement = SendCommand("user.update.ex", "ID=" + bitrixUser.ID + "&UF[EMPGRAD]=" + gradValue.Replace("\"", ""));

        }

        public BitrixCRMCompany GetBitrixCRMCompanyByID(string id)
        {
            BitrixCRMCompany bitrixCRMCompany = null;
            string answer = SendCommand("crm.company.get", "id=" + id);
            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                if (resultData != null
                    && resultData.ContainsKey("result") == true
                    && resultData["result"] != null
                    && String.IsNullOrEmpty(resultData["result"].ToString()) == false)
                {
                    bitrixCRMCompany = JsonConvert.DeserializeObject<BitrixCRMCompany>(resultData["result"].ToString());
                }
            }

            return bitrixCRMCompany;
        }

        public List<BitrixCRMCompany> GetBitrixCRMCompanyList()
        {
            List<BitrixCRMCompany> crmCompanyList = new List<BitrixCRMCompany>();
            string startIndex = null;

            do
            {
                string answer = SendCommand("crm.company.list", ((String.IsNullOrEmpty(startIndex) == true) ? "" : ("&start=" + startIndex)), "");

                string jsonDataArrayString = "";
                if (String.IsNullOrEmpty(answer) == false)
                {
                    Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                    if (resultData != null && resultData.ContainsKey("result") == true)
                    {
                        jsonDataArrayString = resultData["result"].ToString();
                    }

                    if (resultData != null && resultData.ContainsKey("next") == true)
                    {
                        startIndex = resultData["next"].ToString();
                    }
                    else
                    {
                        startIndex = null;
                    }
                }

                if (String.IsNullOrEmpty(jsonDataArrayString) == false)
                {
                    crmCompanyList.AddRange(JsonConvert.DeserializeObject<List<BitrixCRMCompany>>(jsonDataArrayString));
                }

            }
            while (String.IsNullOrEmpty(startIndex) == false);

            return crmCompanyList;
        }

        public string GetBitrixListElementNameByID(string id, string listID)
        {
            string elementName = "";

            string answer = SendCommand("lists.element.get", "IBLOCK_TYPE_ID=lists&IBLOCK_ID=" + listID
                + "&ELEMENT_ID=" + id);

            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                if (resultData != null
                    && resultData.ContainsKey("result") == true
                    && resultData["result"] != null
                    && String.IsNullOrEmpty(resultData["result"].ToString()) == false)
                {
                    List<BitrixListElement> elementList = JsonConvert.DeserializeObject<List<BitrixListElement>>(resultData["result"].ToString());

                    if (elementList != null
                        && elementList.Count == 1)
                    {
                        elementName = elementList[0].NAME;
                    }
                }
            }

            return elementName;
        }

        public string GetBitrixFRCListElementNameByID(string id)
        {
            return GetBitrixListElementNameByID(id, bitrixFRCListID);
        }

        public string GetBitrixCSIListElementNameByID(string id)
        {
            return GetBitrixListElementNameByID(id, bitrixCSIListID);
        }

        public string GetBitrixProjectListElementNameByID(string id)
        {
            return GetBitrixListElementNameByID(id, bitrixProjectsListID);
        }

        public string GetBitrixListElementUrl(string id, string listID)
        {
            string url = bitrixPortalUrl;

            if (bitrixPortalUrl.EndsWith("/") == false)
            {
                url += "/";
            }

            url += "services/lists/" + listID + "/element/0/" + id + "/?list_section_id=";

            return url;
        }



        public BitrixUser GetBitrixUserByID(string id)
        {
            return GetBitrixUserByField("ID", id);
        }

        public BitrixUser GetBitrixUserByEmail(string email)
        {
            return GetBitrixUserByField("EMAIL", email);
        }

        private BitrixUser GetBitrixUserByField(string field, string value)
        {
            BitrixUser bitrixUser = null;
            string answer = SendCommand("user.get", field + "=" + value);
            if (String.IsNullOrEmpty(answer) == false)
            {
                Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                if (resultData != null
                    && resultData.ContainsKey("result") == true
                    && resultData["result"] != null
                    && String.IsNullOrEmpty(resultData["result"].ToString()) == false)
                {
                    List<BitrixUser> bitrixUserList = JsonConvert.DeserializeObject<List<BitrixUser>>(resultData["result"].ToString());

                    if (bitrixUserList != null
                        && bitrixUserList.Count == 1)
                    {
                        bitrixUser = bitrixUserList[0];
                    }
                }
            }

            return bitrixUser;
        }

        public List<BitrixUser> GetBitrixUserList()
        {
            List<BitrixUser> userList = new List<BitrixUser>();
            string startIndex = null;

            do
            {
                string answer = SendCommand("user.get", ((String.IsNullOrEmpty(startIndex) == true) ? "" : ("&start=" + startIndex)), "");

                string jsonDataArrayString = "";
                if (String.IsNullOrEmpty(answer) == false)
                {
                    Dictionary<string, Object> resultData = JsonConvert.DeserializeObject<Dictionary<string, Object>>(answer);
                    if (resultData != null && resultData.ContainsKey("result") == true)
                    {
                        jsonDataArrayString = resultData["result"].ToString();
                    }

                    if (resultData != null && resultData.ContainsKey("next") == true)
                    {
                        startIndex = resultData["next"].ToString();
                    }
                    else
                    {
                        startIndex = null;
                    }
                }

                if (String.IsNullOrEmpty(jsonDataArrayString) == false)
                {
                    userList.AddRange(JsonConvert.DeserializeObject<List<BitrixUser>>(jsonDataArrayString));
                }

            }
            while (String.IsNullOrEmpty(startIndex) == false);

            return userList;
        }

        public BitrixFRC GetBitrixFRCByDepartmentShortName(string departmentShortName)
        {
            return GetBitrixFRCList().FirstOrDefault(i => i.NAME.Split('.').FirstOrDefault() == departmentShortName);
        }

        public BitrixFRC GetBitrixFRCById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixFRC>(bitrixFRCListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixFRC>;
            return result.FirstOrDefault();
        }

        public BitrixDepartment GetBitrixDepartmentById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixDepartment>(bitrixDepartmentListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixDepartment>;
            return result.FirstOrDefault();
        }

        public string GetBitrixDepartmentListID()
        {
            return bitrixDepartmentListID;
        }

        public List<BitrixDepartment> GetBitrixDepartments()
        {
            return GetBitrixList<BitrixDepartment>(bitrixDepartmentListID) as List<BitrixDepartment>;
        }

        public BitrixProject GetBitrixProjectById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixProject>(bitrixProjectsListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixProject>;
            return result.FirstOrDefault();
        }

        public BitrixReqEmployeeEnrollment GetBitrixReqEmployeeEnrollmentById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixReqEmployeeEnrollment>(bitrixReqEmployeeEnrollmentListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixReqEmployeeEnrollment>;
            return result.FirstOrDefault();
        }

        public string GetBitrixReqEmployeeEnrollmentListID()
        {
            return bitrixReqEmployeeEnrollmentListID;
        }

        public List<BitrixReqEmployeeEnrollment> GetBitrixReqEmployeeEnrollmentList()
        {
            return GetBitrixList<BitrixReqEmployeeEnrollment>(bitrixReqEmployeeEnrollmentListID) as List<BitrixReqEmployeeEnrollment>;
        }

        public string GetBitrixReqPayrollChangeListID()
        {
            return bitrixReqPayrollChangeListID;
        }


        public string GetBitrixProjectRoleListID()
        {
            return bitrixProjectRoleListID;
        }

        public BitrixProjectRole GetBitrixProjectRoleById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixProjectRole>(bitrixProjectRoleListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixProjectRole>;
            return result.FirstOrDefault();
        }

        public List<BitrixProjectRole> GetBitrixProjectRoles()
        {
            return GetBitrixList<BitrixProjectRole>(bitrixProjectRoleListID) as List<BitrixProjectRole>;
        }

        public BitrixReqPayrollChange GetBitrixReqPayrollChangeById(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            var result = GetBitrixList<BitrixReqPayrollChange>(bitrixReqPayrollChangeListID, new Dictionary<string, string> { { "ELEMENT_ID", id } }) as List<BitrixReqPayrollChange>;
            return result.FirstOrDefault();
        }

        public List<BitrixReqPayrollChange> GetBitrixReqPayrollChangeList()
        {
            return GetBitrixList<BitrixReqPayrollChange>(bitrixReqPayrollChangeListID) as List<BitrixReqPayrollChange>;
        }
        public string GetBitrixOrganisationListID()
        {
            return bitrixOrganisationListID;
        }

        public List<BitrixOrganisation> GetBitrixOrganisationList()
        {
            return GetBitrixList<BitrixOrganisation>(bitrixOrganisationListID) as List<BitrixOrganisation>;
        }


        public static decimal ParseBitrixListElementDecimalFieldValue(string value)
        {
            decimal result = 0.0M;
            if (String.IsNullOrEmpty(value))
                return result;

            var data = Regex.Split(value.Replace(" ", "").Replace('.', ','), @"[^0-9\,]+").Where(c => c != "," && c.Trim() != "");
            if (data.Count() < 1)
                return result;

            decimal.TryParse(data.First(), out result);
            return result;
        }

        public static double ParseBitrixListElementCurrencyOrNumberFieldValue(string value)
        {
            double result = 0;
            if (String.IsNullOrEmpty(value))
                return result;

            try
            {
                if (value.Contains("|"))
                {
                    result = Convert.ToDouble(value.Split('|')[0].Replace(".", ","));
                }
                else
                {
                    result = Convert.ToDouble(value.Replace("руб.", "").Replace(".", ","));
                }
            }
            catch (Exception)
            {
                result = 0;
            }

            return result;
        }

        public static DateTime ParseBitrixListElementDateFieldValue(string value)
        {
            DateTime result = DateTime.MinValue;
            DateTime.TryParse(value, out result);
            return result;
        }

        public static string ParseBitrixListPropertyDisplayValueByID(Dictionary<string, string> fieldDisplayValuesForm, string value)
        {
            string result = "";

            try
            {
                if (String.IsNullOrEmpty(value) == false
                    && fieldDisplayValuesForm[value] != null)
                {
                    result = Regex.Unescape(fieldDisplayValuesForm[value].Replace("_u", "\\u"));
                }
            }
            catch (Exception)
            {
                result = "";
            }

            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.RecordVersionHistory;

namespace Core.Models
{
    public class BaseModel
    {
        [Display(Name = "ID основной записи")]
        public int? ItemID { get; set; }

        [Display(Name = "Последняя версия")]
        public bool IsVersion { get; set; }

        [Display(Name = "Номер версии")]
        public int? VersionNumber { get; set; }

        [Display(Name = "Создано")]
        public DateTime? Created { get; set; }

        [Display(Name = "Изменено")]
        public DateTime? Modified { get; set; }

        [Display(Name = "Кем создано")]
        public string Author { get; set; }

        [Display(Name = "SID автора в AD")]
        public string AuthorSID { get; set; } //sid учетной записи пользователя в AD

        [Display(Name = "Автор последниих изменений")]
        public string Editor { get; set; }

        [Display(Name = "SID редактора в AD")]
        public string EditorSID { get; set; } //sid учетной записи пользователя в AD

        [Display(Name = "Номер версии")]
        public string DisplayVersionNumber
        {
            get
            {
                if (VersionNumber == null || VersionNumber.HasValue == false)
                {
                    return "1";
                }
                else
                {
                    return (VersionNumber.Value + 1).ToString();
                }
            }
        }

        [Display(Name = "Номер версии")]
        public string DisplayEditor
        {
            get
            {
                if (String.IsNullOrEmpty(Editor) == true)
                {
                    return "<нет данных>";
                }
                else
                {
                    return Editor;
                }
            }
        }

        [Display(Name = "Удаленная запись")]
        public bool IsDeleted { get; set; }

        [Display(Name = "Удалено")]
        public DateTime? DeletedDate { get; set; }

        [Display(Name = "Кем удалено")]
        public string DeletedBy { get; set; }

        [Display(Name = "SID кем удалено в AD")]
        public string DeletedBySID { get; set; }


        public IEnumerable<ChangeInfoRecord> ChangedRecords { get; set; }

        public void InitBaseFields(Tuple<string, string> currentUserInfo)
        {
            Author = currentUserInfo.Item1;
            AuthorSID = currentUserInfo.Item2;
            Editor = currentUserInfo.Item1;
            EditorSID = currentUserInfo.Item2;
            Created = DateTime.Now;
            Modified = DateTime.Now;
            VersionNumber = 0;
            IsVersion = false;
            ItemID = null;
        }

        public void UpdateBaseFields(Tuple<string, string> currentUserInfo, int itemId, BaseModel prevInfo)
        {
            Author = prevInfo.Author;
            AuthorSID = prevInfo.AuthorSID;
            Created = prevInfo.Created;
            Editor = currentUserInfo.Item1;
            EditorSID = currentUserInfo.Item2;
            Modified = DateTime.Now;
            UpdateVersion(prevInfo.VersionNumber);
            IsVersion = false;
            ItemID = itemId;
        }

        public void FreeseVersion<TEntry>(TEntry entry)
        {
            IsVersion = true;
            ItemID = (int)entry.GetType().GetProperty("ID").GetValue(entry, null);
            if (VersionNumber == null || VersionNumber.HasValue == false)
            {
                VersionNumber = 0;
            }
            entry.GetType().GetProperty("ID")?.SetValue(entry, 0);
        }

        //TODO Вызывает ошибку дублированности записей
        //public void FreeseVersion(int itemId)
        //{
        //    IsVersion = true;
        //    ItemID = itemId;
        //    if (VersionNumber == null || VersionNumber.HasValue == false)
        //    {
        //        VersionNumber = 0;
        //    }
        //}

        private void UpdateVersion(int? prevVersion)
        {
            int val = 0;
            if (prevVersion != null && prevVersion.HasValue && prevVersion.Value >= 0)
                val = prevVersion.Value;
            this.VersionNumber = ++val;
        }
    }
}
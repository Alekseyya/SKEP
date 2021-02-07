using System;
using System.Collections.Generic;
using System.Linq;
using Core.BL.Interfaces;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace MainApp.Controllers
{
    public class MigrateController : Controller
    {
        private readonly DbContextOptions<RPCSContext> _dbOptions;
        private readonly DbContextOptions<RPCSContextMysql> _dbMysqOptions;
        private readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;
        private readonly RPCSContext _db;
        private readonly RPCSContextMysql _dbMysql;

        public MigrateController(DbContextOptions<RPCSContext> dbOptions,DbContextOptions<RPCSContextMysql>  dbMysqOptions, ITSAutoHoursRecordService tsAutoHoursRecordService)
        {
            _dbOptions = dbOptions ?? throw new ArgumentNullException(nameof(dbOptions));
            _dbMysqOptions = dbMysqOptions;
            _tsAutoHoursRecordService = tsAutoHoursRecordService;
            _db = new RPCSContext(dbOptions);
            _dbMysql = new RPCSContextMysql(dbMysqOptions);
        }

        public ActionResult Test()
        {
            var result = _db.TSHoursRecords.Where(p => p.ID == 21).ToList();
            var result2 = _db.TSAutoHoursRecords.Where(p => p.ID == 631).ToList();

            return Content("Все заебись");
        }

        public ActionResult CountDate()
        {
            //_db.Filter<BaseModel>(m => m.Where(x => !x.IsVersion));
            //_db.Filter<BaseModel>("IsVersion",m => m.Where(x => !x.IsVersion));
            _db.Filter("IsVersion").Disable();
            var result = "";
            var listIds = _db.TSAutoHoursRecords.Select(x =>  x.ID);
            //var listIds = _tsAutoHoursRecordService.GetAll().Select(x => x.ID);
            foreach (var id in listIds)
            {
                result += " - " + id + "\n";
            }

            result += "Count   " + listIds.Count().ToString();
            return Content(result);


            //var result2 = "";
            //var listIds2 = _db.TSAutoHoursRecords.Select(x => x.ID);
            ////var listIds = _tsAutoHoursRecordService.GetAll().Select(x => x.ID);
            //foreach (var id in listIds2)
            //{
            //    result += " - " + id + "\n";
            //}

            //result2 += "Count   " + listIds2.Count().ToString();

            //return Content(result + "\n" + result2);
        }

        //public ActionResult GetDateFromMysql()
        //{


        //    var result = "";
        //    var listIds = _dbMysql.RPCSUsers.Where(x => x.UserLogin.ToLower() != "ad\\aleksey.yarchuk").Select(x=>x.UserLogin).ToList();
        //    //var listIds = _tsAutoHoursRecordService.GetAll().Select(x => x.ID);
        //    foreach (var id in listIds)
        //    {
        //        result += " - " + id + "\n";
        //    }

        //    result += "Count   " + listIds.Count().ToString();
        //    return Content(result);
        //}


        public ActionResult Migrate()
        {
        //    //С таблицей Employee какие-то проблемы - пользоваться самописным методом
        //    // Project делать через контексты
        //    //С таблицей CostItem какие-то проблемы - пользоваться самописным методом
        //    //С таблицей CostSubItem какие-то проблемы - пользоваться самописным методом
        //    //С таблицей budgetlimit какие-то проблемы - пользоваться самописным методом
        //    //С таблицей vacationrecord какие-то проблемы - пользоваться самописным методом
        //    //С таблицей tsautohoursrecord какие-то проблемы - пользоваться самописным методом
        //    //С таблицей tshoursrecord какие-то проблемы - пользоваться самописным методом




        //    //foreach (var entity in tshoursrecord)
        //    //{
        //    //    _db.TSHoursRecords.Add(entity);
        //    //    _db.SaveChanges();
        //    //}

        //    //return Content("Миграция для бд " + tshoursrecord.GetType().ToString());

        dynamic entities;
            try
            {
                entities = _dbMysql.TSHoursRecords.ToList();
            }
            catch (Exception e)
            {
                return Content("Код ошибки" + e);
}

            foreach (var entity in entities)
            {
                _db.TSHoursRecords.Add(entity);
                _db.SaveChanges();
            }

            return Content("Миграция для бд " + entities.GetType().ToString());
        }
    }
}
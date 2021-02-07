﻿using System;
using Data;
using Data.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MainApp.Common
{
    [ApiController]
    public abstract class DataAwareApiController : Controller
    {
        private readonly DbContextOptions<RPCSContext> _dbContextOptions;
        private RPCSSingletonDbAccessor _dbAccessor;

        public DataAwareApiController(DbContextOptions<RPCSContext> dbContextOptions)
        {
            _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
        }
        protected IRPCSDbAccessor DbAccessor
        {
            get { return _dbAccessor; }
        }

        protected DataAwareApiController()
        {
            // TODO: надо использовать IoC механизм (например, Unity) для получения зависимостей
            _dbAccessor = new RPCSSingletonDbAccessor(_dbContextOptions);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_dbAccessor != null)
                {
                    _dbAccessor.Dispose();
                    _dbAccessor = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>日志控制器</summary>
    [DisplayName("日志")]
    [Description("系统内重要操作均记录日志，便于审计。任何人都不能删除、修改或伪造操作日志。")]
    public class LogController : EntityController<XLog>
    {
        static LogController()
        {
            // 日志列表需要显示详细信息，不需要显示用户编号
            ListFields.AddField("Action", "Remark");
            ListFields.RemoveField("CreateUserID");
            FormFields.RemoveField("Remark");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override EntityList<XLog> FindAll(Pager p)
        {
            return XLog.Search(p["Q"], p["adminid"].ToInt(), p["category"], p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(XLog entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改日志");
        }

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Edit(XLog entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改日志");
        }

        /// <summary>不允许删除日志</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Delete(Int32 id)
        {
            //return base.Delete(id);
            throw new Exception("不允许删除日志");
        }

        /// <summary>不允许删除日志</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName()]
        public override JsonResult DeleteAjax(Int32 id)
        {
            var url = Request.UrlReferrer + "";

            return Json(new { msg = "不允许删除日志！", code = -1, url = url }, JsonRequestBehavior.AllowGet);
        }

        ///// <summary>获取可用于生成权限菜单的Action集合</summary>
        ///// <param name="menu"></param>
        ///// <returns></returns>
        //protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        //{
        //    var dic = base.ScanActionMenu(menu);

        //    dic = dic.Where(e => !e.Key.Name.EqualIgnoreCase("Add", "Edit", "Delete")).ToDictionary(e => e.Key, e => e.Value);

        //    return dic;
        //}

        public ActionResult EnableClear()
        {
            // Gateway.Meta.Session.Execute("Update Gateway set Online=0");
            XLog.Meta.Session.Execute("DELETE FROM Log");
            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Swastika.Messenger.Mvc.Controllers
{
    public class BaseController<T> : Controller
    {
        public readonly string ROUTE_CULTURE_NAME = "culture";
        public readonly string ROUTE_DEFAULT_CULTURE = "vi-vn";
        protected string _domain;
        protected IHostingEnvironment _env;
        private string _currentLanguage;

        public BaseController(IHostingEnvironment env)
        {
            _env = env;
            string lang = RouteData != null && RouteData.Values[ROUTE_CULTURE_NAME] != null
               ? RouteData.Values[ROUTE_CULTURE_NAME].ToString() : ROUTE_DEFAULT_CULTURE;

            // Set CultureInfo
            var cultureInfo = new CultureInfo(CurrentLanguage);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }

        public ViewContext ViewContext { get; set; }

        protected string CurrentLanguage
        {
            get
            {
                _currentLanguage = RouteData?.Values[ROUTE_CULTURE_NAME] != null
                                    ? RouteData.Values[ROUTE_CULTURE_NAME].ToString().ToLower() : ROUTE_DEFAULT_CULTURE.ToLower();
                return _currentLanguage;
            }
        }

        //public BaseController(IHostingEnvironment env, IStringLocalizer<SharedResource> localizer)
        //{
        //    _env = env;
        //    string lang = RouteData != null && RouteData.Values[CONST_ROUTE_CULTURE_NAME] != null
        //        ? RouteData.Values[CONST_ROUTE_CULTURE_NAME].ToString() : CONST_ROUTE_DEFAULT_CULTURE;
        //    listCultures = listCultures ?? CultureRepository.GetInstance().GetModelList();
        //}

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //if (!string.IsNullOrEmpty(GlobalConfigurationService.Instance.GetConnectionString()))
            //{
            //    GetLanguage();
            //}
            GetLanguage();
            base.OnActionExecuting(context);
        }

        protected void GetLanguage()
        {
            //_lang = RouteData != null && RouteData.Values[CONST_ROUTE_CULTURE_NAME] != null
            //    ? RouteData.Values[CONST_ROUTE_CULTURE_NAME].ToString() : CONST_ROUTE_DEFAULT_CULTURE;

            _domain = string.Format("{0}://{1}", Request.Scheme, Request.Host);

            ViewBag.culture = CurrentLanguage;

            //ViewBag.currentCulture = listCultures.FirstOrDefault(c => c.Specificulture == _lang);
            //ViewBag.cultures = listCultures;
        }

       
    }
}

﻿using GoData.Core.Logic;
using GoData.Portal.PageViewModels;
using GoData.Portal.PageViewModels.SettingsViewModels;
using Microsoft.AspNetCore.Mvc;
using System;

namespace GoData.Portal.Controllers
{
    public class SettingsController : Controller
    {
        private OrganizationLogic _organizationLogic;
        private UserLogic _userLogic;
        BasePageViewModel<DefaultPageViewModel> _viewModel;

        public SettingsController(
            OrganizationLogic organizationLogic,
            UserLogic userLogic)
        {
            _organizationLogic = organizationLogic;
            _userLogic = userLogic;
        }

        public IActionResult Index()
        {
            var userId = Int32.Parse(Request.Headers["User"].ToString());

            var pageModel = new IndexPageViewModel();


            pageModel.Organizations = _organizationLogic.GetOrganizationsByUserId(userId);

            return View(pageModel);
        }

        [HttpPost]
        public IActionResult SwitchOrganization(int organizationId)
        {
            var userId = Request?.Headers["User"];

            var user = _userLogic.GetUserById(Int32.Parse(userId.Value));

            user.DefaultOrganization = organizationId;

            _userLogic.UpdateUserAsync(user);

            return RedirectToAction("Index", "Home");
        }
    }
}
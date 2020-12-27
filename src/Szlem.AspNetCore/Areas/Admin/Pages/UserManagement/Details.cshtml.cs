using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Szlem.Engine;
using Szlem.Engine.UserManagement;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Admin.UserManagement.Pages
{
    public class DetailsModel : PageModel
    {
        public const string PageName = "Details";
        public static readonly string Route = $"/{Consts.SubAreaName}/{PageName}";
        public const string GrantRoleActionName = "GrantRole";
        public const string RevokeRoleActionName = "RevokeRole";
        public const string DeleteUserActionName = "DeleteUser";
        public const string ChangeNameActionName = "ChangeName";

        private readonly ISzlemEngine _app;

        public DetailsModel(ISzlemEngine app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }


        public DetailsUseCase.UserDetails UserDetails { get; set; }
        public SelectList AvailableRoles { get; set; }

        public async Task<IActionResult> OnGet(Guid userId)
        {
            UserDetails = await _app.Query(new DetailsUseCase.Query() { UserId = userId });

            var availableRoles = await _app.Query(new DetailsUseCase.GetRolesThatCanBeGrantedQuery() { UserId = userId });
            AvailableRoles = new SelectList(availableRoles, nameof(DetailsUseCase.RoleData.Name), nameof(DetailsUseCase.RoleData.Description));

            return Page();
        }


        #region AddRole()

        [BindProperty]
        public GrantRoleUseCase.Command GrantRoleCommand { get; set; }

        public async Task<IActionResult> OnPostGrantRole()
        {
            await _app.Execute(GrantRoleCommand);
            return await OnGet(GrantRoleCommand.UserID);
        }

        #endregion

        #region RemoveRole()

        [BindProperty]
        public RevokeRoleUseCase.Command RevokeRoleCommand { get; set; }

        public async Task<IActionResult> OnPostRevokeRole()
        {
            await _app.Execute(RevokeRoleCommand);
            return await OnGet(RevokeRoleCommand.UserID);
        }

        #endregion

        #region DeleteUser()

        [BindProperty]
        public DeleteUseCase.Command DeleteUserCommand { get; set; }

        public async Task<IActionResult> OnPostDeleteUser()
        {
            await _app.Execute(DeleteUserCommand);
            return RedirectToPage(IndexModel.Route);
        }

        #endregion

        #region ChangeName()

        [BindProperty]
        public ChangeNameUseCase.Command ChangeNameCommand { get; set; }

        public async Task<IActionResult> OnPostChangeName()
        {
            await _app.Execute(ChangeNameCommand);
            return RedirectToPage(IndexModel.Route);
        }

        #endregion
    }
}
using Cairo_Cinema.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cairo_Cinema.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
            {
                return View(registerVM);
            }

            ApplicationUser user = new ApplicationUser()
            {
                Name = registerVM.Name,
                Address = registerVM.Address,
                UserName = registerVM.UserName,
                Email = registerVM.Email,
            };
            var result = await _userManager.CreateAsync(user,registerVM.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(registerVM);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail),"Account" , new {area = "Identity" , id = user.Id , token },Request.Scheme);
            await _emailSender.SendEmailAsync(
                registerVM.Email,
                "Cairo_Cinema Confirmation Email",
                $"<h3>Please Click <a href='{link}'>here</a> to confirm your Email</h3>"
            );

            TempData["Successfully"] = "Register Successfully";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string id, string token)
        {
            var user = await _userManager.FindByIdAsync(id);
            var result = await _userManager.ConfirmEmailAsync(user,token);
            if (!result.Succeeded)
            {
                TempData["Error_Validation"] = "Invalid Confirmation Email";
                return RedirectToAction(nameof(Login));
            }
            TempData["Successfully"] = "Email Confirmation Successfully";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVm)
        {
            var user = await _userManager.FindByEmailAsync(loginVm.UserNameOrEmail) ?? 
                       await _userManager.FindByNameAsync(loginVm.UserNameOrEmail);
            if(user == null)
            {
                ModelState.AddModelError("", "Invalid UserName Or Email Or Password");
                return View(loginVm);
            }
            var result = await _signInManager.PasswordSignInAsync(user,loginVm.Password,loginVm.RememberMe,true);
            if (!result.Succeeded) {
                if (result.IsLockedOut)
                {
                ModelState.AddModelError("", "To Many attempts pls try again later");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("", "Please Confirm your Email First");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid UserName Or Email Or Password");
                }
                return View(loginVm);
            }
            TempData["Successfully"] = "Login Successfully";
            return RedirectToAction("Index","Home", new {area = "Customer" });

        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.UserNameOrEmail) ??
            await _userManager.FindByNameAsync(resendEmailConfirmationVM.UserNameOrEmail);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid UserName Or Email Or Password");
                return View(resendEmailConfirmationVM);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Email Already Confirmed");
                return View(resendEmailConfirmationVM);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", id = user.Id, token }, Request.Scheme);
            await _emailSender.SendEmailAsync(
                user.Email,
                "Cairo_Cinema Confirmation Email",
                $"<h3>Please Click <a href='{link}'>here</a> to confirm your Email</h3>"
            );
            return RedirectToAction(nameof(Login));
        }
    }
}
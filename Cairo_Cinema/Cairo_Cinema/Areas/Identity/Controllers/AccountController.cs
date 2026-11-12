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
        private readonly IRepository<ApplicationUserOtp> _applicationUserOtpRepository;

        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, IRepository<ApplicationUserOtp> applicationUserOtp)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _applicationUserOtpRepository = applicationUserOtp;
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

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            var user = await _userManager.FindByEmailAsync(forgetPasswordVM.UserNameOrEmail) ??
                       await _userManager.FindByNameAsync(forgetPasswordVM.UserNameOrEmail);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid UserName Or Email Or Password");
                return View(forgetPasswordVM);
            }

            var otp = new Random().Next(1000 , 9999).ToString();
            var applicationUserOtp = new ApplicationUserOtp(otp ,user.Id);
            await _applicationUserOtpRepository.AddAsync(applicationUserOtp);
            await _applicationUserOtpRepository.CommitAsync();
            await _emailSender.SendEmailAsync(
                user.Email,
                "Cairo_Cinema Reset password",
                $"<h3>Use This {otp} to Reset Your Password</h3>"
            );

            return RedirectToAction(nameof(VerifyOtp), new {userId = user.Id});
        }

        [HttpGet]
        public IActionResult VerifyOtp(string userId)
        {
            return View(new VerifyOtpVM(){ UserId = userId});
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpVM verifyOtpVM)
        {
            var user = await _userManager.FindByIdAsync(verifyOtpVM.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid User");
                return View(verifyOtpVM);
            }
            var applicationUserOtps = await _applicationUserOtpRepository.GetListAsync( a=>
                a.ApplicationUserId == user.Id && 
                a.IsValid &&
                a.ValidTo > DateTime.UtcNow
            );
            var applicationUserOtp = applicationUserOtps.OrderBy(a=>a.ApplicationUserId).LastOrDefault();
            if (applicationUserOtp == null || applicationUserOtp.OTP != verifyOtpVM.OTP) {

                ModelState.AddModelError("", "Invalid OTP / Expired");
                return View(verifyOtpVM);

            }
            applicationUserOtp.IsValid = false;
            await _applicationUserOtpRepository.CommitAsync();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction(nameof(ResetPassword), new {userId = user.Id, token});
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId ,string token)
        {
            return View(new ResetPasswordVM() { UserId = userId,Token = token});
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordVM);
            }
            var user = await _userManager.FindByIdAsync(resetPasswordVM.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid User");
                return View(resetPasswordVM);
            }
            var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(resetPasswordVM);
            }
            TempData["Successfully"] = "Reset Password Successfully";
            return RedirectToAction(nameof(Login));

        }

    }
}
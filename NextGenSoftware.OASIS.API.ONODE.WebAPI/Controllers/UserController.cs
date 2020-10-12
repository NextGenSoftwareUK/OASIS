//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Authorization;
//using System.Threading.Tasks;
//using WebAPI.Models;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Authorize]
//    [ApiController]
//    [Route("api/[controller]")]
//    public class UsersController : ControllerBase
//    {
//        private IAvatarService _userService;

//        public UsersController(IAvatarService userService)
//        {
//            _userService = userService;
//        }

//        [AllowAnonymous]
//        [HttpPost("authenticate")]
//        public async Task<IActionResult> Authenticate([FromBody]AuthenticateModel model)
//        {
//            var user = await _userService.Authenticate(model.Username, model.Password);

//            if (user == null)
//                return BadRequest(new { message = "Username or password is incorrect" });

//            return Ok(user);
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            var users = await _userService.GetAll();
//            return Ok(users);
//        }
//    }
//}

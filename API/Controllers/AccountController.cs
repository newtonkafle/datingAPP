using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entity;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseController
    {
        public DataContext _context { get; set; }
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService )
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto){

            if(await UserExists(registerDto.userName)) return BadRequest("username is taken ");
            
            using var hmac = new HMACSHA512();

            var user = new AppUser{

                userName = registerDto.userName.ToLower(),
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                passwordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

        return new UserDto{

            userName = user.userName,
            Token = _tokenService.CreateToken(user)
        };
        }

        [HttpPost("login")]
        public async  Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
            .SingleOrDefaultAsync( x => x.userName == loginDto.Username);

            if(user ==null) return Unauthorized("Invalid Username"); //checking fot the username for login

            //decreapting password

            using var hmac = new HMACSHA512(user.passwordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i=0; i<computedHash.Length; i++)
            {
                if(computedHash[i] != user.passwordHash[i]) return Unauthorized("invalid password");
            }

            return new UserDto{
                userName = user.userName,
                 Token = _tokenService.CreateToken(user)
            };


        }

        private async Task<bool> UserExists(string username){

            return await _context.Users.AnyAsync(x => x.userName == username.ToLower());
        }
    }
}
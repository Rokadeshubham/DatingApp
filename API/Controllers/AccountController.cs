using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context,ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        //[HttpPost("register")]
        //public async Task<ActionResult<AppUser>> Register(string username, string password)
        //{
        //    using var hmac = new HMACSHA512();
        //    //sending username and password hash and salt to db
        //    var user = new AppUser
        //    {
        //        UserName = username,
        //        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
        //        PasswordSalt = hmac.Key
        //    };

        //    _context.Users.Add(user);

        //    await _context.SaveChangesAsync();

        //    return user;
        //}
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDtos registerDto)
        {
            if (await UserExists(registerDto.Username))
            {
                return BadRequest("Username is taken");
            }
            using var hmac = new HMACSHA512();
            //sending username and password hash and salt to db
            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            }; 

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return new UserDto {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        //Check Username is already available or not
        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
            if (user == null)
            {
                return Unauthorized("Invalid Username");
                
            }
            using var hmac = new HMACSHA512(user.PasswordSalt);
            //Check User entered passowrd and Original Password is same or not
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                {
                    return Unauthorized("Invalid Password");
                }


            }
            return  new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }






    }
}

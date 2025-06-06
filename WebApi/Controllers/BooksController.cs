﻿using Infrastructure.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    public class BooksController(UserManager<UserEntity> userManager) : Controller
    {
        [HttpGet]
        public IActionResult GetBook()
        {
           return Ok(new { title = "Book Title", author = "Author Name" });
        }

    }
}
